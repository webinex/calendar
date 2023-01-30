using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Repeats;

internal static class RepeatEventCalculator
{
    public static Period[] Matches(RecurrentEvent @event, DateTimeOffset start, DateTimeOffset end)
    {
        return new MatchCalculator(@event, start.ToUtc(), end.ToUtc()).Calculate();
    }

    public static Period[] Interval(RecurrentEvent @event, DateTimeOffset from, DateTimeOffset to)
    {
        var date = FindFirstMatchingIntervalStart(@event, from);
        var periods = new LinkedList<Period>();

        while (IsEffective(@event, date) && InRange(from, to, date, @event.Repeat.Interval!.DurationMinutes))
        {
            var eventStart = date;
            var eventEnd = eventStart.AddMinutes(@event.Repeat.Interval!.DurationMinutes);
            periods.AddLast(new Period(eventStart, eventEnd));
            date = date.AddMinutes(@event.Repeat.Interval.IntervalMinutes);
        }

        return periods.ToArray();
    }

    private static bool IsEffective(RecurrentEvent @event, DateTimeOffset start)
    {
        return start >= @event.Effective.Start &&
               (!@event.Effective.End.HasValue || @event.Effective.End.Value > start);
    }

    private static bool InRange(DateTimeOffset from, DateTimeOffset to, DateTimeOffset start, int durationMinutes)
    {
        return start < to && start.AddMinutes(durationMinutes) > from;
    }

    private static DateTimeOffset FindFirstMatchingIntervalStart(RecurrentEvent @event, DateTimeOffset from)
    {
        var fromSinceJ1990 = (from - Constants.J1_1990).TotalMinutes;
        var interval = @event.Repeat.Interval!;
        var sinceStartOfInterval = fromSinceJ1990 - interval.StartSince1990Minutes;

        if (sinceStartOfInterval < 0)
            return Constants.J1_1990.AddMinutes(interval.StartSince1990Minutes);

        var previous = from.AddMinutes(-1 * (sinceStartOfInterval % interval.IntervalMinutes));
        return previous.AddMinutes(interval.DurationMinutes) >= from
            ? previous
            : previous.AddMinutes(interval.IntervalMinutes);
    }

    private class MatchCalculator
    {
        private readonly RecurrentEvent _event;
        private readonly RepeatMatch _match;
        private readonly DateTimeOffset _from;
        private readonly DateTimeOffset _to;

        private DateTimeOffset? _reference;
        private readonly LinkedList<Period> _matches = new();

        public MatchCalculator(RecurrentEvent @event, DateTimeOffset from, DateTimeOffset to)
        {
            _event = @event;
            _match = @event.Repeat.Match!;
            _from = from;
            _to = to;

            _reference = from;
        }

        public Period[] Calculate()
        {
            _reference = null;
            _matches.Clear();

            while (Next())
            {
                _matches.AddLast(new Period(_reference!.Value, _reference.Value.AddMinutes(_match.DurationMinutes)));
            }

            return _matches.ToArray();
        }

        private bool Next()
        {
            if (!_reference.HasValue)
                return MoveStart();

            return MoveNext(_reference.Value);
        }

        private bool MoveStart()
        {
            if (IsPreviousDateMatch())
            {
                MoveStartToPreviousDateMatch();
                return true;
            }

            if (IsStartDateMatch())
            {
                MoveStartToStartDateMatch();
                return true;
            }

            return MoveNext(_from.StartOfTheDayUtc());
        }

        private bool MoveNext(DateTimeOffset value)
        {
            var next = FindNextMatch(value.StartOfTheDayUtc().AddDays(1));
            if (next.HasValue)
                _reference = next;

            return next.HasValue;
        }

        private void MoveStartToStartDateMatch()
        {
            var match = _from.StartOfTheDayUtc().AddMinutes(_match.TimeOfTheDayUtcMinutes);
            _reference = match;
        }

        private bool IsPreviousDateMatch()
        {
            var eventStart = _from.AddDays(-1).AddMinutes(_match.TimeOfTheDayUtcMinutes);

            return (Match(Weekday.From(_from.DayOfWeek).Previous()) || Match(_from.AddDays(-1).Day)) &&
                   _match.OvernightDurationMinutes.HasValue &&
                   _from.TotalMinutesFromStartOfTheDayUtc() < _match.OvernightDurationMinutes &&
                   IsEffective(eventStart) &&
                   InRange(eventStart);
        }

        private void MoveStartToPreviousDateMatch()
        {
            var match = _from.StartOfTheDayUtc().AddDays(-1).AddMinutes(_match.TimeOfTheDayUtcMinutes);
            _reference = match;
        }

        private bool IsStartDateMatch()
        {
            var eventStart = _from.StartOfTheDayUtc().AddMinutes(_match.TimeOfTheDayUtcMinutes);
            return (Match(Weekday.From(_from.DayOfWeek)) || Match(_from.Day))
                   && _from.TotalMinutesFromStartOfTheDayUtc() < _match.SameDayLastTime
                   && IsEffective(eventStart)
                   && InRange(eventStart);
        }

        private DateTimeOffset? FindNextMatch(DateTimeOffset value)
        {
            var nextDay = value;
            while (nextDay < _to)
            {
                if (!Match(Weekday.From(nextDay.DayOfWeek)) && !Match(nextDay.Day))
                {
                    nextDay = nextDay.AddDays(1);
                    continue;
                }

                var match = nextDay.AddMinutes(_match.TimeOfTheDayUtcMinutes);
                if (match >= _to || (_event.Effective.End.HasValue && match > _event.Effective.End))
                    break;

                if (!IsEffective(match))
                {
                    nextDay = nextDay.StartOfTheDayUtc().AddDays(1);
                    continue;
                }

                return match;
            }

            return null;
        }

        private bool IsEffective(DateTimeOffset start)
        {
            return RepeatEventCalculator.IsEffective(_event, start);
        }

        private bool InRange(DateTimeOffset start)
        {
            return RepeatEventCalculator.InRange(_from, _to, start, _match.DurationMinutes);
        }

        private bool Match(Weekday weekday) => _match.Weekdays.Contains(weekday);
        private bool Match(int dayOfMonth) => _match.DayOfMonth == new DayOfMonth(dayOfMonth);
    }
}