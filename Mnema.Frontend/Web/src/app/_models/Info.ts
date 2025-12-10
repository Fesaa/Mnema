import {Provider} from "./page";


export type SearchInfo = {
  Name: string;
  Description: string;
  Size: string;
  Tags: InfoTag[];
  Link: string;
  InfoHash: string;
  ImageUrl: string;
  RefUrl: string;
  Provider: Provider;
}

export type InfoTag = {
  Name: string;
  Value: any;
}
