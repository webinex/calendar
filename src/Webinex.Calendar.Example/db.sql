drop table if exists Events;

create table Events
(
    Id                                    uniqueidentifier not null
        constraint PK_Events primary key,
    DateTime                              datetimeoffset   null,
    Effective_Start                       datetimeoffset   not null,
    Effective_End                         datetimeoffset   null,
    Type                                  int              not null,
    RecurrentEventId                      uniqueidentifier null
        constraint FK_Events_RecurrentEventId_Events_Id foreign key references Events (Id),

    Repeat_Type                           int              null,
    Repeat_Interval_StartSince1990Minutes bigint           null,
    Repeat_Interval_EndSince1990Minutes   bigint           null,
    Repeat_Interval_IntervalMinutes       int              null,
    Repeat_Interval_DurationMinutes       int              null,

    Repeat_Match_TimeOfTheDayUtcMinutes   int              null,
    Repeat_Match_DurationMinutes          int              null,
    Repeat_Match_OvernightDurationMinutes int              null,
    Repeat_Match_SameDayLastTime          int              null,
    Repeat_Match_Monday                   bit              null,
    Repeat_Match_Tuesday                  bit              null,
    Repeat_Match_Wednesday                bit              null,
    Repeat_Match_Thursday                 bit              null,
    Repeat_Match_Friday                   bit              null,
    Repeat_Match_Saturday                 bit              null,
    Repeat_Match_Sunday                   bit              null,
    Repeat_Match_DayOfMonth               int              null,
    Data_Name                             nvarchar(200)    not null,
)