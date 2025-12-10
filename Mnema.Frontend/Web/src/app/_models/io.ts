export type ListDirRequest = {
  dir: string;
  files: boolean;
}

export type DirEntry = {
  name: string;
  dir: boolean;
}

export type CreateDirRequest = {
  baseDir: string;
  newDir: string;
}
