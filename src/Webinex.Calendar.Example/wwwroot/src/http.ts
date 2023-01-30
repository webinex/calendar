import { AxiosInstance } from 'axios';

export type Weekday =
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'
  | 'Sunday';

export type EventType = 'OneTime' | 'Match' | 'Interval';

export interface Event<TData> {
  id?: string;
  recurringEventId?: string;
  start: string;
  end: string;
  data: TData;
}

export interface CreateEventRequest<TData> {
  oneTime?: CreateOneTimeEventPayload<TData>;
  match?: CreateMatchEventPayload<TData>;
  interval?: CreateIntervalEventPayload<TData>;
}

export interface CreateOneTimeEventPayload<TData> {
  start: string;
  end: string;
  data: TData;
}

export interface CreateMatchEventPayload<TData> {
  start: string;
  end: string | null;
  timeOfTheDayUtcMinutes: number;
  durationMinutes: number;
  weekdays: Weekday[] | null;
  dayOfMonth: number | null;
  data: TData;
}

export interface CreateIntervalEventPayload<TData> {
  start: string;
  end: string | null;
  durationMinutes: number;
  intervalMinutes: number;
  data: TData;
}

export class CalendarHttp<TData> {
  private _axios: AxiosInstance;

  constructor(axios: AxiosInstance) {
    if (axios == null) throw new Error('`axios` cannot be null');

    this._axios = axios;
  }

  public async fetch(from: Date, to: Date): Promise<Event<TData>[]> {
    const { data } = await this._axios.get<Event<TData>[]>(
      `?from=${from.toISOString()}&to=${to.toISOString()}`,
    );
    return data;
  }

  public async create(request: CreateEventRequest<TData>): Promise<string> {
    const { data } = await this._axios.post<string>('', request);
    return data;
  }
}
