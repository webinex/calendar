import { Form, Modal } from 'antd';
import { Formik } from 'formik';
import { useCallback } from 'react';
import * as Yup from 'yup';
import { calendarHttpApp } from './calendarHttpApp';
import { FormsDate } from './Form/FormsDate';
import { FormsInput } from './Form/FormsInput';
import { FormsNumber } from './Form/FormsNumber';
import { FormsRadio } from './Form/FormsRadio';
import { FormsSelect } from './Form/FormsSelect';
import { EventType, Weekday } from './http';

const SCHEMA = Yup.object({
  title: Yup.string().nullable().required(),
  type: Yup.mixed<EventType>()
    .oneOf(['OneTime', 'Match', 'Interval'])
    .required(),
  start: Yup.string().nullable().required(),
  end: Yup.string()
    .nullable()
    .defined()
    .when('type', { is: 'OneTime', then: (x) => x.required() }),
  timeOfTheDayUtcMinutes: Yup.number()
    .nullable()
    .when('type', {
      is: 'Match',
      then: (x) => x.required(),
    }),
  durationMinutes: Yup.number()
    .nullable()
    .defined()
    .when('type', ([type], schema) =>
      ['Match', 'Interval'].includes(type) ? schema.required() : schema,
    ),
  weekdays: Yup.array()
    .of(Yup.mixed<Weekday>().required())
    .when('dayOfMonth', { is: null, then: (x) => x.required() }),
  dayOfMonth: Yup.number().defined().nullable(),
  intervalMinutes: Yup.number()
    .nullable()
    .defined()
    .when('type', { is: 'Interval', then: (x) => x.required() }),
});

type FormValue = Yup.InferType<typeof SCHEMA>;

const INTIIAL_VALUE: FormValue = {
  title: '',
  type: 'OneTime',
  start: null!,
  end: null!,
  timeOfTheDayUtcMinutes: null!,
  durationMinutes: null!,
  weekdays: [],
  dayOfMonth: null!,
  intervalMinutes: null!,
};

function useSubmit({ onSubmitted }: CreateEventModalProps) {
  return useCallback(
    (value: FormValue) => {
      if (value.type === 'OneTime') {
        calendarHttpApp
          .create({
            oneTime: {
              start: value.start!,
              end: value.end!,
              data: { name: value.title },
            },
          })
          .then(onSubmitted);
      } else if (value.type === 'Match') {
        calendarHttpApp
          .create({
            match: {
              start: value.start!,
              end: value.end,
              data: { name: value.title },
              dayOfMonth: value.dayOfMonth,
              durationMinutes: value.durationMinutes!,
              timeOfTheDayUtcMinutes: value.timeOfTheDayUtcMinutes!,
              weekdays: value.weekdays!,
            },
          })
          .then(onSubmitted);
      } else if (value.type === 'Interval') {
        calendarHttpApp
          .create({
            interval: {
              start: value.start!,
              end: value.end,
              data: { name: value.title },
              intervalMinutes: value.intervalMinutes!,
              durationMinutes: value.durationMinutes!,
            },
          })
          .then(onSubmitted);
      }
    },
    [onSubmitted],
  );
}

export interface CreateEventModalProps {
  onClose: () => any;
  onSubmitted: () => any;
}

export function CreateEventModal(props: CreateEventModalProps) {
  const { onClose } = props;
  const handleSubmit = useSubmit(props);

  return (
    <Formik
      initialValues={INTIIAL_VALUE}
      validationSchema={SCHEMA}
      onSubmit={handleSubmit}
    >
      {(formik) => {
        const { values, submitForm } = formik;
        const { type } = values;

        return (
          <Modal title="Create event" open onOk={submitForm} onCancel={onClose}>
            <Form layout="vertical">
              <FormsRadio
                name="type"
                options={[
                  { label: 'One time', value: 'OneTime' },
                  { label: 'Match', value: 'Match' },
                  { label: 'Interval', value: 'Interval' },
                ]}
              />
              <FormsInput name="title" label="Title" required />
              <FormsDate name="start" label="Start" showTime required />
              <FormsDate
                name="end"
                label="End"
                showTime
                required={type === 'OneTime'}
                placeholder={type !== 'OneTime' ? 'Empty is never' : undefined}
              />
              {type === 'Match' && (
                <>
                  <FormsNumber
                    name="timeOfTheDayUtcMinutes"
                    label="Time of the day (UTC minutes)"
                    required
                  />
                  <FormsNumber
                    name="durationMinutes"
                    label="Duration (minutes)"
                    required
                  />
                  <FormsSelect
                    name="weekdays"
                    mode="multiple"
                    label="Weekdays"
                    options={[
                      { label: 'Monday', value: 'Monday' },
                      { label: 'Tuesday', value: 'Tuesday' },
                      { label: 'Wednesday', value: 'Wednesday' },
                      { label: 'Thursday', value: 'Thursday' },
                      { label: 'Friday', value: 'Friday' },
                      { label: 'Saturday', value: 'Saturday' },
                      { label: 'Sunday', value: 'Sunday' },
                    ]}
                  />
                  <FormsNumber name="dayOfMonth" label="Day of month" />
                </>
              )}

              {type === 'Interval' && (
                <>
                  <FormsNumber
                    name="intervalMinutes"
                    label="Interval (minutes)"
                    required
                  />
                  <FormsNumber
                    name="durationMinutes"
                    label="Duration (minutes)"
                    required
                  />
                </>
              )}
            </Form>
          </Modal>
        );
      }}
    </Formik>
  );
}
