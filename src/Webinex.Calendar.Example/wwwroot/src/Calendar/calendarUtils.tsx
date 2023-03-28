import moment from 'moment';
import {
  DateRange,
  NavigateAction,
  stringOrDate,
  View,
} from 'react-big-calendar';

export function convertDateRange(datesOrRange: Date[] | DateRange): DateRange {
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

export function inRange(
  range: { start: stringOrDate; end: stringOrDate },
  nested: { start: stringOrDate; end: stringOrDate },
) {
  return (
    moment(nested.start).isSameOrAfter(moment(range.start)) &&
    moment(nested.end).isSameOrBefore(moment(range.end))
  );
}

function convertViewToMomentDuration(view: View) {
  switch (view) {
    case 'week':
      return 'week';
    case 'month':
      return 'month';
    case 'day':
      return 'day';
    default:
      throw new Error(`Unable to convert ${view}`);
  }
}

function convertViewToMomentStartOf(view: View) {
  switch (view) {
    case 'week':
      return 'isoWeek';
    case 'month':
      return 'month';
    case 'day':
      return 'day';
    default:
      throw new Error(`Unable to convert ${view}`);
  }
}

export function convertNavigateToRange(
  newDate: Date,
  view: View,
  action: NavigateAction,
): DateRange {
  if (action === 'TODAY') {
    return {
      start: moment(newDate).startOf(convertViewToMomentStartOf(view)).toDate(),
      end: moment(newDate)
        .startOf(convertViewToMomentStartOf(view))
        .add(1, convertViewToMomentDuration(view))
        .toDate(),
    };
  }

  return {
    start: newDate,
    end: moment(newDate).add(1, convertViewToMomentDuration(view)).toDate(),
  };
}

export function convertViewToRange(currentRange: DateRange, view: View): DateRange {
  const start = moment(currentRange.start)
    .startOf(convertViewToMomentStartOf(view))
    .toDate();

  return {
    start,
    end: moment(start).add(1, convertViewToMomentDuration(view)).toDate(),
  };
}
