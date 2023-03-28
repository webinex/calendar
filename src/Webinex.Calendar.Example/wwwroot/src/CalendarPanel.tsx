import moment from 'moment';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { CreateEventModal, CreateEventValue } from './CreateEventModal';
import { calendarHttp, EditEventTimeRequest, Event } from './http';
import { Calendar, CalendarEvent, useCalendarRange } from './Calendar';
import { DateRange } from 'react-big-calendar';
import {
  EditEventTimeFormValue,
  EditEventTimeModal,
} from './EditEventTimeModal';
import { CancelEventModal } from './CancelEventModal';
import {
  CancelRecurrentEventFormValue,
  CancelRecurrentEventModal,
} from './CancelRecurrentEventModal';

function useFetch(setEvents: (events: CalendarEvent[]) => any) {
  return useCallback(
    (range: DateRange) => {
      calendarHttp
        .fetch(
          moment(range!.start).startOf('minute').toDate(),
          moment(range!.end).startOf('minute').toDate(),
        )
        .then((events) => {
          const calendarEvents: CalendarEvent[] = events.map((event) => ({
            id:
              event.id ??
              `${event.recurringEventId}+${moment(
                event.start,
              ).toISOString()}+${moment(event.end).toISOString()}`,
            start: moment(event.start).toDate(),
            end: moment(event.end).toDate(),
            title: event.data.title,
            data: event.data,
            original: event,
          }));

          setEvents(calendarEvents);
        });
    },
    [setEvents],
  );
}

function useViewState() {
  const [events, setEvents] = useState<CalendarEvent[]>([]);
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

function useCreateModal(fetch: (range: DateRange) => any, range: DateRange) {
  const [showCreateModal, setShowCreateModal] = useState(false);

  const onCreate = useCallback(
    (value: CreateEventValue) => {
      calendarHttp.create(value).then(() => {
        fetch(range);
        setShowCreateModal(false);
      });
    },
    [fetch, range],
  );

  const onCancelCreate = useCallback(() => setShowCreateModal(false), []);
  const onShowCreate = useCallback(() => setShowCreateModal(true), []);

  return { showCreateModal, onCreate, onShowCreate, onCancelCreate };
}

function useEditTimeModal(fetch: (range: DateRange) => any, range: DateRange) {
  const [editTimeEvent, setEditTimeEvent] = useState<Event>();

  const onEditTime = useCallback(
    (value: EditEventTimeFormValue) => {
      const request: EditEventTimeRequest = {
        eventStart: editTimeEvent!.movedFrom?.start ?? editTimeEvent!.start,
        recurrentEventId: editTimeEvent!.recurringEventId!,
        moveToStart: value.start,
        moveToEnd: value.end,
      };

      calendarHttp.editTime(request).then(() => {
        fetch(range);
        setEditTimeEvent(undefined);
      });
    },
    [fetch, range, editTimeEvent],
  );

  const onCancelEditTime = useCallback(() => setEditTimeEvent(undefined), []);
  const onShowEditTime = useCallback(
    (event: Event) => setEditTimeEvent(event),
    [],
  );

  const initialEditEventTimeValues = useMemo<EditEventTimeFormValue | null>(
    () =>
      editTimeEvent
        ? { start: moment(editTimeEvent.start), end: moment(editTimeEvent.end) }
        : null,
    [editTimeEvent],
  );

  return {
    showEditTimeModal: !!editTimeEvent,
    onEditTime,
    onShowEditTime,
    onCancelEditTime,
    initialEditEventTimeValues,
  };
}

function useCancelEventModal(
  fetch: (range: DateRange) => any,
  range: DateRange,
) {
  const [cancelEvent, setCancelEvent] = useState<Event>();

  const onHideCancelEvent = useCallback(() => setCancelEvent(undefined), []);
  const onCancelEventSubmit = useCallback(() => {
    const promise = cancelEvent!.recurringEventId
      ? calendarHttp.cancelAppearance({
          recurrentEventId: cancelEvent!.recurringEventId!,
          eventStart: cancelEvent?.movedFrom?.start
            ? moment(cancelEvent.movedFrom.start)
            : cancelEvent!.start,
        })
      : calendarHttp.cancelOneTime({ id: cancelEvent!.id! });

    promise.then(() => {
      fetch(range);
      setCancelEvent(undefined);
    });
  }, [cancelEvent, fetch, range]);

  return {
    onShowCancelEvent: setCancelEvent,
    onHideCancelEvent,
    onCancelEventSubmit,
    showCancelEvent: !!cancelEvent,
    cancelEvent,
  };
}

function useCancelRepeatModal(reload: () => any) {
  const [event, setEvent] = useState<Event>();
  const onHideCancelRepeat = useCallback(() => setEvent(undefined), []);

  const onSubmitCancelRepeat = useCallback(
    (value: CancelRecurrentEventFormValue) =>
      calendarHttp
        .cancelRecurrent({
          recurrentEventId: event!.recurringEventId!,
          since: value.since,
        })
        .then(() => {
          reload();
          setEvent(undefined);
        }),
    [reload, event],
  );

  return {
    cancelRepeatEvent: event,
    showCancelRepeat: !!event,
    onShowCancelRepeat: setEvent,
    onHideCancelRepeat,
    onSubmitCancelRepeat,
  };
}

export function CalendarPanel() {
  const { events, fetch } = useViewState();
  const { range, onRangeChange } = useCalendarRange();
  const reload = useCallback(() => fetch(range), [fetch, range]);

  useEffect(() => {
    fetch(range);
  }, [range]);

  const { showCreateModal, onCreate, onCancelCreate, onShowCreate } =
    useCreateModal(fetch, range);

  const {
    onCancelEditTime,
    onEditTime,
    onShowEditTime,
    showEditTimeModal,
    initialEditEventTimeValues,
  } = useEditTimeModal(fetch, range);

  const {
    onCancelEventSubmit,
    onHideCancelEvent,
    onShowCancelEvent,
    showCancelEvent,
    cancelEvent,
  } = useCancelEventModal(fetch, range);

  const {
    onHideCancelRepeat,
    onShowCancelRepeat,
    onSubmitCancelRepeat,
    showCancelRepeat,
    cancelRepeatEvent,
  } = useCancelRepeatModal(reload);

  return (
    <div>
      {showCreateModal && (
        <CreateEventModal onCreate={onCreate} onCancel={onCancelCreate} />
      )}

      {showEditTimeModal && (
        <EditEventTimeModal
          initialValues={initialEditEventTimeValues!}
          onCancel={onCancelEditTime}
          onSubmit={onEditTime}
        />
      )}

      {showCancelEvent && (
        <CancelEventModal
          event={cancelEvent!}
          onCancel={onHideCancelEvent}
          onConfirm={onCancelEventSubmit}
        />
      )}

      {showCancelRepeat && (
        <CancelRecurrentEventModal
          eventTitle={cancelRepeatEvent!.data.title}
          onCancel={onHideCancelRepeat}
          onSubmit={onSubmitCancelRepeat}
        />
      )}

      <Calendar
        events={events}
        onRangeChange={onRangeChange}
        range={range}
        onEditTimeClick={onShowEditTime}
        onAddClick={onShowCreate}
        onCancelAppearanceClick={onShowCancelEvent}
        onCancelRepeatClick={onShowCancelRepeat}
      />
    </div>
  );
}
