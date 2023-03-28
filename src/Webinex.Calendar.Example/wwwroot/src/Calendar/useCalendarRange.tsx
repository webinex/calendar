import moment from 'moment';
import { useState } from 'react';
import { DateRange } from 'react-big-calendar';

export function useCalendarRange() {
  const [range, setRange] = useState<DateRange>({
    start: moment().startOf('isoWeek').toDate(),
    end: moment().startOf('isoWeek').add(1, 'week').toDate(),
  });

  return { range, onRangeChange: setRange };
}