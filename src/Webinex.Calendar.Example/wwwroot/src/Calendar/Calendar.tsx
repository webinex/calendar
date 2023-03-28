import 'react-big-calendar/lib/css/react-big-calendar.css';
import styles from './Calendar.module.scss';

import React, { useCallback, useMemo, useState } from 'react';
import {
  Calendar as _ReactBigCalendar,
  CalendarProps as ReactBigCalendarProps,
  Components,
  DateRange,
  NavigateAction,
  View,
} from 'react-big-calendar';
import { calendarLocalizaer } from './calendarLocalizer';
import { CalendarEvent } from './calendarTypes';
import { CustomToolbar } from './CustomToolbar';
import { createCustomEventWrapper } from './CustomEventWrapper';
import classNames from 'classnames';
import { convertNavigateToRange, convertViewToRange } from './calendarUtils';
import { Event } from '@/http';
import { Button } from 'antd';

function useCustomComponents(
  props: CalendarProps,
): Components<CalendarEvent, object> {
  const { onEditTimeClick, onCancelAppearanceClick, onCancelRepeatClick } =
    props;

  return useMemo(
    () => ({
      toolbar: CustomToolbar,
      eventWrapper: createCustomEventWrapper({
        onEditTimeClick,
        onCancelAppearanceClick,
        onCancelRepeatClick,
      }),
    }),
    [onEditTimeClick, onCancelAppearanceClick, onCancelRepeatClick],
  );
}

const ReactBigCalendar: React.ComponentType<
  ReactBigCalendarProps<CalendarEvent, object>
> = _ReactBigCalendar as any;

export interface CalendarProps {
  range: DateRange;
  onRangeChange: (value: DateRange) => any;
  events: CalendarEvent[];
  onEditTimeClick: (event: Event) => any;
  onAddClick: () => any;
  onCancelAppearanceClick: (event: Event) => any;
  onCancelRepeatClick: (event: Event) => any;
}

const VIEWS: View[] = ['day', 'week', 'month'];

function useNavigate(props: CalendarProps) {
  const { onRangeChange } = props;

  return useCallback(
    (newDate: Date, view: View, action: NavigateAction) => {
      const range = convertNavigateToRange(newDate, view, action);
      onRangeChange(range);
    },
    [onRangeChange],
  );
}

function useView(props: CalendarProps) {
  const { range, onRangeChange } = props;
  const [view, setView] = useState<View>('week');

  const onViewChange = useCallback(
    (view: View) => {
      setView(view);
      const newRange = convertViewToRange(range, view);
      onRangeChange(newRange);
    },
    [range, onRangeChange],
  );

  return { view, onViewChange };
}

export function Calendar(props: CalendarProps) {
  const { events, range, onAddClick } = props;
  const navigate = useNavigate(props);
  const { view, onViewChange } = useView(props);
  const customComponents = useCustomComponents(props);

  return (
    <div className={styles.container}>
      <ReactBigCalendar
        date={range.start}
        onNavigate={navigate}
        className={classNames(styles.calendar, 'Calendar')}
        events={events}
        localizer={calendarLocalizaer}
        components={customComponents}
        view={view}
        views={VIEWS}
        onView={onViewChange}
        showAllEvents
      />

      <Button onClick={onAddClick} className={styles.addBtn}>
        +
      </Button>
    </div>
  );
}
