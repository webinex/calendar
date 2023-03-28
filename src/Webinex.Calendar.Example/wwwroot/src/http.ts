import axios from 'axios';
import { Moment } from 'moment';

export type Weekday =
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'
  | 'Sunday';

const ORDERED_WEEKDAY: Weekday[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

export function addWeekday(weekday: Weekday, value: number): Weekday {
  const index = ORDERED_WEEKDAY.indexOf(weekday);
  return ORDERED_WEEKDAY.at((index + value) % ORDERED_WEEKDAY.length)!;
}

export type EventType =
  | 'OneTime'
  | 'RepeatWeekday'
  | 'RepeatDayOfMonth'
  | 'RepeatInterval';

export interface Period {
  start: string;
  end: string;
}

export interface Event {
  id?: string;
  recurringEventId?: string;
  start: string;
  end: string;
  data: EventData;
  movedFrom?: Period;
}

export interface EventData {
  title: string;
}

export interface CreateEventRequest {
  type: EventType;
  title: string;
  start: Moment;
  end?: Moment;
  weekdays?: Weekday[];
  dayOfMonth?: number;
  timeOfTheDayUtcMinutes?: number;
  durationMinutes?: number;
  intervalMinutes?: number;
}

export interface EditEventTimeRequest {
  recurrentEventId: string;
  eventStart: string | Moment | Date;
  moveToStart: string | Moment | Date;
  moveToEnd: string | Moment | Date;
}

export interface CancelRecurrentEventAppearanceRequest {
  recurrentEventId: string;
  eventStart: string | Moment | Date;
}

export interface CancelOneTimeEventRequest {
  id: string;
}

export interface CancelRecurrentEventRequest {
  recurrentEventId: string;
  since: string | Moment | Date;
}

export class CalendarHttp {
  private _axios = axios.create({ baseURL: '/api/calendar' });

  public async fetch(from: Date, to: Date): Promise<Event[]> {
    const { data } = await this._axios.get<Event[]>(
      `?from=${from.toISOString()}&to=${to.toISOString()}`,
    );
    return data;
  }

  public async create(request: CreateEventRequest): Promise<string> {
    const { data } = await this._axios.post<string>('', request);
    return data;
  }

  public async editTime(request: EditEventTimeRequest): Promise<void> {
    await this._axios.put('time', request);
  }

  public async cancelAppearance(
    request: CancelRecurrentEventAppearanceRequest,
  ): Promise<void> {
    await this._axios.put('cancel/appearance', request);
  }

  public async cancelOneTime(
    request: CancelOneTimeEventRequest,
  ): Promise<void> {
    await this._axios.put('cancel/one-time', request);
  }

  public async cancelRecurrent(
    request: CancelRecurrentEventRequest,
  ): Promise<void> {
    await this._axios.put('cancel/recurrent', request);
  }
}

export const calendarHttp = new CalendarHttp();
