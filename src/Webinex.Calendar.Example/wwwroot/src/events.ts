import moment from "moment";
import { Event } from "react-big-calendar";

export interface CalendarEvent<TData> extends Event {
  id: string;
  data: TData
}

export interface EventData {
  name: string;
}

export const EVENTS: CalendarEvent<EventData>[] = [
  {
    start: moment.utc("2023-01-30T13:00:00.000Z").toDate(),
    end: moment.utc("2023-01-30T14:00:00.000Z").toDate(),
    title: "Event 1",
    id: "1",
    data: {
      name: "Event 1",
    }
  },
];
