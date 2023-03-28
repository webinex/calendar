drop table if exists Events;
create table Events
(
    Id                                    uniqueidentifier not null
    constraint PK_Events primary key,
    Effective_Start                       bigint           not null,
    Effective_End                         bigint           null,
    Type                                  int              not null,
    RecurrentEventId                      uniqueidentifier null
    constraint FK_Events_RecurrentEventId_Events_Id foreign key references Events (Id),
    Cancelled                             bit              not null,
    MoveTo_Start                          datetimeoffset   null,
    MoveTo_End                            datetimeoffset   null,

    Repeat_Type                           int              null,
    Repeat_IntervalMinutes                int              null,
    Repeat_DurationMinutes                int              null,
    Repeat_TimeOfTheDayUtcMinutes         int              null,
    Repeat_OvernightDurationMinutes       int              null,
    Repeat_SameDayLastTime                int              null,
    Repeat_Monday                         bit              null,
    Repeat_Tuesday                        bit              null,
    Repeat_Wednesday                      bit              null,
    Repeat_Thursday                       bit              null,
    Repeat_Friday                         bit              null,
    Repeat_Saturday                       bit              null,
    Repeat_Sunday                         bit              null,
    Repeat_DayOfMonth                     int              null,
    Data_Title                            nvarchar(200)    not null,
);