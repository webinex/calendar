import {
  DATE_RANGE_FORMAT,
  HOUR_FORMAT,
  HOUR_WITH_LEADING_ZERO_FORMAT_MOMENT,
} from '@/system';
import moment from 'moment';
import { momentLocalizer } from 'react-big-calendar';

export const calendarLocalizaer = momentLocalizer(moment);

export const FORMATS = {
  eventTime: HOUR_FORMAT,
  timeslotTime: HOUR_WITH_LEADING_ZERO_FORMAT_MOMENT,
};

export const calendarDateRangeView = (calendarLocalizaer.formats.dayRangeHeaderFormat = (
  range,
) =>
  range.start.getMonth() === range.end.getMonth()
    ? `${calendarLocalizaer.format(range.start, 'DD')} - ${calendarLocalizaer.format(
        range.end,
        DATE_RANGE_FORMAT,
      )}`
    : `${calendarLocalizaer.format(range.start, 'DD MMMM')} - ${calendarLocalizaer.format(
        range.end,
        DATE_RANGE_FORMAT,
      )}`);

(calendarLocalizaer.formats as any).eventTime = HOUR_FORMAT;
