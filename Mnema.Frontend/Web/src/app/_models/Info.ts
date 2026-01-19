import {Provider} from "./page";


export type SearchInfo = {
  id: string;
  downloadUrl: string;
  name: string;
  description: string;
  size: string;
  tags: string[];
  imageUrl: string;
  url: string;
  provider: Provider;
}
