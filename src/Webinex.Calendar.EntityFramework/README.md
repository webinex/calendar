# Webinex.Calendar.EntityFramework

## Design

The main idea to create two separate tables for events/exceptions and recurrent event is to optimize indexes.
As for recurrent event, we cannot perform index optimized query.

Effective period for recurrent events can be two big, and we cannot use index for period intersection. For
one time events and occurencies we can assume that event period cannot be greater than 1d, and we can disallow to
move event for more than 1d from it original time. In this case, we can use a range of 2d prior requested period start
and less than period end. In this case, search period is strict and can use index by effective start.

Btw, we cannot use index for recurrent event, but when we move recurrent events into separate table we'll
reduce a size of this table and clustered index scan will take fewer efforts.