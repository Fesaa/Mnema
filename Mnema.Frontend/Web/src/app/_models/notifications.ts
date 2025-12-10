export interface Notification {
  ID: number;
  title: string;
  summary: string;
  body: string;
  colour: NotificationColour;
  group: NotificationGroup;
  read: boolean;
  readAt?: Date;
  CreatedAt: Date;
}

export enum NotificationColour {
  Primary = "primary",
  Secondary = "secondary",
  Warn = "warning",
  Error = "error"
}

export enum NotificationGroup {
  Content = "content",
  Security = "security",
  General = "general",
  Error = "error",
}

export function GroupWeight(group: NotificationGroup): number {
  switch (group) {
    case NotificationGroup.Security:
      return 10;
    case NotificationGroup.Error:
      return 5;
    case NotificationGroup.General:
      return 2;
    case NotificationGroup.Content:
      return 0;
  }
}
