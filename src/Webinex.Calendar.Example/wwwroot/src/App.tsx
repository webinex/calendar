import './App.css';
import 'react-big-calendar/lib/css/react-big-calendar.css';
import 'antd/dist/antd.css';
import { Calendar, momentLocalizer } from 'react-big-calendar';
import { CalendarEvent, EventData } from './events';
import moment from 'moment';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Divider } from 'antd';
import { CreateEventModal } from './CreateEventModal';
import { calendarHttpApp } from './calendarHttpApp';

const localizer = momentLocalizer(moment);

interface Range {
  start: Date;
  end: Date;
}

function convertDateRange(datesOrRange: Date[] | Range): Range {
  if (!Array.isArray(datesOrRange)) return datesOrRange;

  if (datesOrRange.length === 1)
    return {
      start: moment(datesOrRange[0]).startOf('day').toDate(),
      end: moment(datesOrRange[0]).endOf('day').toDate(),
    };

  return {
    start: moment(datesOrRange[0]).startOf('day').toDate(),
    end: moment(datesOrRange.at(-1)).endOf('day').toDate(),
  };
}

function useFetch(setEvents: (events: CalendarEvent<EventData>[]) => any) {
  const lastRef = useRef<Range>();

  return useCallback(
    (datesOrRange?: Date[] | Range) => {
      if (!datesOrRange && !lastRef.current) {
        throw new Error(`datesOrRange required when called for a first time`);
      }

      const range = datesOrRange ? convertDateRange(datesOrRange) : lastRef.current!;
      lastRef.current = range;

      calendarHttpApp.fetch(range.start, range.end).then((events) => {
        const calendarEvents: CalendarEvent<EventData>[] = events.map(
          (event) => ({
            id:
              event.id ??
              `${event.recurringEventId}+${moment(
                event.start,
              ).toISOString()}+${moment(event.end).toISOString()}`,
            start: moment(event.start).toDate(),
            end: moment(event.end).toDate(),
            title: event.data.name,
            data: event.data,
          }),
        );

        setEvents(calendarEvents);
      });
    },
    [setEvents],
  );
}

function useViewState() {
  const [events, setEvents] = useState<CalendarEvent<EventData>[]>([]);
  const fetch = useFetch(setEvents);

  useEffect(() => {
    fetch({
      start: moment().startOf('month').toDate(),
      end: moment().endOf('month').toDate(),
    });

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return {
    events,
    fetch,
  };
}

function App() {
  const { events, fetch } = useViewState();
  const [showCreateModal, setShowCreateModal] = useState(false);

  return (
    <div className="App">
      <Button type="primary" onClick={() => setShowCreateModal(true)}>
        Create Event
      </Button>
      {showCreateModal && (
        <CreateEventModal
          onClose={() => setShowCreateModal(false)}
          onSubmitted={() => {
            setShowCreateModal(false);
            fetch();
          }}
        />
      )}

      <Divider />

      <Calendar
        className="Calendar"
        events={events}
        localizer={localizer}
        startAccessor="start"
        endAccessor="end"
        onRangeChange={fetch}
        defaultView="month"
      />
    </div>
  );
}

export default App;
