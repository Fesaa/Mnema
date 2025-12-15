export interface Notification {
  id: number;
  title: string;
  summary: string;
  body: string;
  colour: NotificationColour;
  read: boolean;
  readAt?: Date;
  createdUtc: Date;
}

export enum NotificationColour {
  Primary = 0,
  Secondary = 1,
  Warn = 2,
  Error = 3,
}
