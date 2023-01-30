import axios from 'axios';
import { EventData } from './events';
import { CalendarHttp } from './http';

const axiosInstance = axios.create({ baseURL: '/api/calendar' });
export const calendarHttpApp = new CalendarHttp<EventData>(axiosInstance)