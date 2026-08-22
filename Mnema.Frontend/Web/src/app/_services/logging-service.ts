import {environment} from "@env/environment";

export enum LogLevel {
  DEBUG,
  INFO,
  WARN,
  ERROR,
}

export abstract class LoggingService {

  abstract name: string;
  abstract logLevel: LogLevel;

  private isLogLevelEnabled(level: LogLevel) {
    return level >= this.logLevel;
  }

  protected log(message: string, ...data: any[]) {
    if (!this.isLogLevelEnabled(LogLevel.DEBUG) || environment.production) {
      return;
    }

    if (data !== undefined && data.length > 0) {
      console.debug(`[${this.name}] ${message}`, ...data);
    } else {
      console.debug(`[${this.name}] ${message}`);
    }
  }

  protected info(message: string, ...data: any[]) {
    if (!this.isLogLevelEnabled(LogLevel.INFO)) {
      return;
    }

    if (data !== undefined && data.length > 0) {
      console.warn(`[${this.name}] ${message}`, ...data);
    } else {
      console.warn(`[${this.name}] ${message}`);
    }
  }

  protected warn(message: string, ...data: any[]) {
    if (!this.isLogLevelEnabled(LogLevel.WARN)) {
      return;
    }

    if (data !== undefined && data.length > 0) {
      console.warn(`[${this.name}] ${message}`, ...data);
    } else {
      console.warn(`[${this.name}] ${message}`);
    }
  }

  protected error(message: string, ...data: any[]) {
    if (!this.isLogLevelEnabled(LogLevel.ERROR)) {
      return;
    }

    if (data !== undefined && data.length > 0) {
      console.warn(`[${this.name}] ${message}`, ...data);
    } else {
      console.warn(`[${this.name}] ${message}`);
    }
  }

}
