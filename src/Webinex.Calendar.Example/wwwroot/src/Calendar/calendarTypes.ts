import { Event } from 'react-big-calendar';
import { EventData, Event as EventModel } from '@/http';

export interface CalendarEvent extends Event {
  id: string;
  data: EventData;
  original: EventModel;
}
