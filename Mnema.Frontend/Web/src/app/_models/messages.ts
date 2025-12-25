import {Provider} from "./page";

export type Message<T> = {
  provider: Provider;
  contentId: string;
  type: MessageType;
  data?: T;
}

export type ListContentData = {
  subContentId?: string;
  label: string;
  selected: boolean;
  children: ListContentData[];
}

export enum MessageType {
  MessageListContent = 0,
  SetToDownload = 1,
  StartDownload = 2,
}
