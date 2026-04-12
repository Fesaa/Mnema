# Set up

Mnema is a containerized service that exposes a web interface on **port 8080**. To ensure proper data persistence and functionality, the container requires a volume mount at `/persistent` to house the Mangabaka database and its search index.

Configuration follows the standard .NET pattern, offering two flexible methods for initialization:
* **Configuration File:** Mount a custom `appsettings.json` to `/Mnema/config/appsettings.json`.
* **Environment Variables:** Provide settings directly via the container environment.

You may use both methods simultaneously; a common best practice is to define general settings in the JSON file while injecting sensitive credentials (like secrets and connection strings) via environment variables.

## Configuration Reference

> **Logging:** You can fully configure application logging using **Serilog** via the `Serilog` configuration section or corresponding environment variables.

| Variable (JSON Path)         |   | Description                                                                                                                                                                   |
|------------------------------|:--|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Connections**              |   |                                                                                                                                                                               |
| `ConnectionStrings:Redis`    |   | Connection string for the Redis cache.                                                                                                                                        |
| `ConnectionStrings:Postgres` |   | Connection string for the PostgreSQL database.                                                                                                                                |
| `ConnectionStrings:Sqlite`   |   | Connection string for the Sqlite database, ensure this maps to a persistent location. Postgres takes priority. Example: `Data Source=/persistent/Mnema.db;` The ; is required | 
| **Authentication**           |   |                                                                                                                                                                               |
| `OpenIdConnect:Authority`    |   | The OIDC Identity Provider URL.                                                                                                                                               |
| `OpenIdConnect:ClientId`     |   | The registered Client ID for the application.                                                                                                                                 |
| `OpenIdConnect:Secret`       |   | The secret key for OIDC handshake.                                                                                                                                            |
| `NoAuthentication`           |   | Set the `true` if you wish to disable Authentication                                                                                                                          |
| `Authentication:Hardcover`   |   | Your hardcover ApiKey, required if you wish to use hardcover as a metadata provider                                                                                           |
| **Storage**                  |   |                                                                                                                                                                               |
| `Application:BaseDir`        |   | **Required.** The root directory for media storage.                                                                                                                           |
| `Application:DownloadDir`    |   | **Required.** The directory used for processing downloads.                                                                                                                    |
| **System & Libs**            |   |                                                                                                                                                                               |
| `TZ`                         |   | Sets the system timezone for the runtime (e.g., `Europe/Brussels`).                                                                                                           |
| `AutoMapperLicense`          |   | Optional license key. Displays a warning on start if missing and not suppressed.                                                                                              |


<warning>
    It is advised to use OIDC to secure your application, when disabling authentication make sure Mnema is not publicly accesible
</warning>

---

### Key Mapping Rules

* **Nesting:** Use `__` (double underscore) to represent nested JSON objects in environment variables (e.g., `OpenIdConnect__Secret`).
* **Secrets:** Sensitive values like PostgreSQL strings and OIDC secrets should be injected via a secret management system.

## Docker compose example

<code-block lang="yaml" language="yaml">
services:
  mnema:
    image: ghcr.io/fesaa/mnema:latest
    container_name: mnema
    ports:
      - "8080:8080"
    environment:
      - TZ=Europe/Brussels
      - ConnectionStrings__Postgres=Host=postgres;Database=mnema;Username=mnema_user;Password=your_secure_password
      - OpenIdConnect__Secret=your_oidc_secret
      # - AutoMapperLicense=your_license_here
    volumes:
      - ./data/persistent:/persistent
      - /path/to/your/media:/media
      - /path/to/your/downloads:/downloads
     - ./appsettings.json:/Mnema/config/appsettings.json #Check in the kubernetes section for what should be in here
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started
    restart: unless-stopped

  postgres:
    image: postgres:17.4-alpine
    container_name: mnema-db
    environment:
      - POSTGRES_DB=mnema
      - POSTGRES_USER=mnema_user
      - POSTGRES_PASSWORD=your_secure_password
    volumes:
      - ./data/postgres:/var/lib/postgresql/data

  redis:
    image: redis:8.2.2-alpine
    container_name: mnema-redis
    volumes:
      - ./data/redis:/data
    restart: unless-stopped
</code-block>

## Kubernetes example

I run my software in a kubernetes cluster, so this is how I run mine

<tabs>
    <tab title="ConfigMap">
        <code-block lang="yaml">
apiVersion: v1
kind: ConfigMap
metadata:
  name: mnema-appsettings
data:
  appsettings.json: |
    {
      "Serilog": {
        "MinimumLevel": {
          "Default": "Error",
          "Override": {
            "Mnema": "Debug"
          }
        }
      },
      "ConnectionStrings": {
        "Redis": "redis-svc.common.svc.cluster.local"
      },
      "OpenIdConnect": {
        "Authority": "https://sso.example.com/realms/prod",
        "ClientId": "mnema"
      },
      "Application": {
        "BaseDir": "/media",
        "DownloadDir": "/downloads",
        "PersistentStorage": "/persistent",
        "Host": "https://example.com"
      }
    }
        </code-block>
    </tab>
    <tab title="PersistentVolumeClaim">
        <code-block lang="yaml">
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mnema-persistent
  namespace: media-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
        </code-block>
    </tab>
    <tab title="Deployment">
        <code-block lang="yaml">
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mnema
  namespace: media-management
  labels:
    app: mnema
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mnema
  template:
    metadata:
      name: mnema
      labels:
        app: mnema
    spec:
      containers:
        - name: mnema
          image: ghcr.io/fesaa/mnema:latest
          imagePullPolicy: Always
          ports:
            - containerPort: 8080
              protocol: TCP
          securityContext:
            runAsUser: 1000
            runAsGroup: 1000
          resources:
            limits:
              cpu: "4"
              memory: "4Gi"
            requests:
              cpu: "1"
              memory: "500Mi"
          env:
            - name: TZ
              value: Europe/Brussels
          envFrom:
            - secretRef:
                name: mnema-settings
          volumeMounts:
            - name: media
              mountPath: /media
            - name: downloads
              mountPath: /downloads
            - name: config
              mountPath: /Mnema/config/appsettings.json
              subPath: appsettings.json
            - name: persistent
              mountPath: /persistent
      restartPolicy: Always
      volumes:
        - name: media
          hostPath:
            path: /mnt/storage/Media
        - name: downloads
          hostPath:
            path: /mnt/storage/downloads
        - name: config
          configMap:
            name: mnema-appsettings
        - name: persistent
          persistentVolumeClaim:
            claimName: mnema-persistent
        </code-block>
    </tab>
</tabs>
