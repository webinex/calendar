import {
  Checkbox,
  Col,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Radio,
  Row,
  Select,
  TimePicker,
} from 'antd';
import { FormInstance } from 'antd/es/form/Form';
import moment, { Moment } from 'moment';
import { useCallback, useEffect } from 'react';
import { addWeekday, EventType, Weekday } from './http';
import { required, rule } from './rule';
import {
  DATE_FORMAT,
  DATE_TIME_FORMAT,
  DEFAULT_ROW_GUTTER,
  MOMENT_START_OF_DAY,
  TIME_FORMAT,
} from './system';

export interface CreateEventValue {
  type: EventType;
  title: string;
  start: Moment;
  end?: Moment;
  weekdays?: Weekday[];
  dayOfMonth?: number;
  timeOfTheDayUtcMinutes?: number;
  durationMinutes?: number;
  intervalMinutes?: number;
}

export interface CreateEventModalProps {
  onCancel: () => any;
  onCreate: (value: CreateEventValue) => any;
}

type IntervalType = 'minutes' | 'hours' | 'days' | 'weeks';

const INTERVAL_TYPE_TO_MINUTE_MULTIPLIER: Record<IntervalType, number> = {
  minutes: 1,
  hours: 1 * 60,
  days: 24 * 60,
  weeks: 24 * 60 * 7,
};

interface FormValue {
  type: EventType;
  title: string;
  start: Moment;
  end: Moment | null;
  weekdays: Weekday[];
  dayOfMonth: number | null;
  timeOfTheDayUtcMinutes: Moment | null;
  durationMinutes: number | null;
  intervalValue: number | null;
  intervalType: IntervalType | null;
}

const INITIAL_VALUE: FormValue = {
  type: 'OneTime',
  title: '',
  start: null!,
  end: null,
  weekdays: [],
  dayOfMonth: null,
  durationMinutes: null,
  intervalType: null,
  intervalValue: null,
  timeOfTheDayUtcMinutes: null,
};

const DEFAULT_VALUES: Record<EventType, Partial<FormValue>> = {
  RepeatDayOfMonth: { dayOfMonth: 1 },
  RepeatInterval: { intervalType: 'days', intervalValue: 1 },
  RepeatWeekday: {
    weekdays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
  },
  OneTime: {},
};

function useSubmit(
  props: CreateEventModalProps,
  form: FormInstance<FormValue>,
) {
  const { onCreate } = props;

  return useCallback(() => {
    form.validateFields().then((value) => {
      let weekdays = value.weekdays;
      let dayOfMonth = value.dayOfMonth;

      let timeOfTheDayUtcMinutes = value.timeOfTheDayUtcMinutes?.diff(
        moment(value.timeOfTheDayUtcMinutes).startOf('day'),
        'minutes',
      );

      let start =
        value.type === 'OneTime'
          ? moment(value.start).startOf('minute')
          : moment(value.start).startOf('date');

      if (
        value.type !== 'OneTime' &&
        timeOfTheDayUtcMinutes! < moment().utcOffset()
      ) {
        timeOfTheDayUtcMinutes = 24 * 60 - (moment().utcOffset() - timeOfTheDayUtcMinutes!);
        weekdays = weekdays?.map(weekday => addWeekday(weekday, -1));

        // TODO: dayOfMonth should be corrected as well
      } else if (
        value.type !== 'OneTime' &&
        timeOfTheDayUtcMinutes! >= moment().utcOffset()
      ) {
        timeOfTheDayUtcMinutes = timeOfTheDayUtcMinutes! - moment().utcOffset();
      }

      const end =
        value.type === 'OneTime'
          ? moment(value.end).startOf('minute')
          : moment(value.end).startOf('date').add(1, 'day');

      onCreate({
        start,
        end,
        title: value.title,
        type: value.type,
        dayOfMonth: value.dayOfMonth ?? undefined,
        durationMinutes: value.durationMinutes ?? undefined,
        intervalMinutes: value.intervalType
          ? INTERVAL_TYPE_TO_MINUTE_MULTIPLIER[value.intervalType!] *
            value.intervalValue!
          : undefined,
        timeOfTheDayUtcMinutes,
        weekdays: weekdays ?? undefined,
      });
    });
  }, [form]);
}

export function CreateEventModal(props: CreateEventModalProps) {
  const { onCancel } = props;
  const [form] = Form.useForm<FormValue>();
  const handleSubmit = useSubmit(props, form);
  const { setFieldsValue } = form;
  const type = Form.useWatch<EventType>('type', form);
  const start = Form.useWatch('start', form);

  const is = useCallback(
    (...expectedType: EventType[]) => expectedType.some((t) => t === type),
    [type],
  );

  useEffect(() => {
    setFieldsValue({
      ...INITIAL_VALUE,
      ...DEFAULT_VALUES[type],
      type,
      title: form.getFieldValue('title'),
    });
  }, [type]);

  return (
    <Modal
      open
      title="Create event"
      okText="Create"
      onOk={handleSubmit}
      onCancel={onCancel}
    >
      <Form
        name="create-event"
        form={form}
        initialValues={INITIAL_VALUE}
        layout="vertical"
      >
        <Form.Item label="Title" name="title" required rules={[required()]}>
          <Input />
        </Form.Item>

        <Form.Item name="type" label="Type" required>
          <Radio.Group>
            <Radio value="OneTime">One time</Radio>
            <Radio value="RepeatWeekday">Weekday</Radio>
            <Radio value="RepeatDayOfMonth">Day of month</Radio>
            <Radio value="RepeatInterval">Interval</Radio>
          </Radio.Group>
        </Form.Item>

        <Row gutter={20} wrap={false}>
          <Col span={12}>
            <Form.Item name="start" label="Start" rules={[required()]}>
              <DatePicker
                showTime={
                  is('OneTime') ? { defaultValue: MOMENT_START_OF_DAY } : false
                }
                showSecond={false}
                format={is('OneTime') ? DATE_TIME_FORMAT : DATE_FORMAT}
                className="w-100"
              />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="end"
              label="End"
              dependencies={['type', 'start']}
              rules={[
                required(type === 'OneTime'),
                rule(
                  (value) =>
                    !value || !moment(value).isSameOrBefore(moment(start)),
                  'End might be after start',
                ),
                rule(
                  (value) =>
                    !value ||
                    moment(value).diff(moment(start), 'minutes') < 24 * 60,
                  'Event cannot be longer than 24h',
                ),
              ]}
            >
              <DatePicker
                className="w-100"
                showSecond={false}
                format={is('OneTime') ? DATE_TIME_FORMAT : DATE_FORMAT}
                showTime={
                  is('OneTime') ? { defaultValue: MOMENT_START_OF_DAY } : false
                }
              />
            </Form.Item>
          </Col>
        </Row>

        {is('RepeatWeekday') && (
          <Form.Item
            name="weekdays"
            label="Weekdays"
            required
            rules={[
              rule(
                (value: string[]) => value.length > 0,
                'At least one weekday might be selected',
              ),
            ]}
          >
            <Checkbox.Group>
              <Checkbox value="Monday">Mon</Checkbox>
              <Checkbox value="Tuesday">Tue</Checkbox>
              <Checkbox value="Wednesday">Wed</Checkbox>
              <Checkbox value="Thursday">Thu</Checkbox>
              <Checkbox value="Friday">Fri</Checkbox>
              <Checkbox value="Saturday">Sat</Checkbox>
              <Checkbox value="Sunday">Sun</Checkbox>
            </Checkbox.Group>
          </Form.Item>
        )}

        {is('RepeatDayOfMonth') && (
          <Row>
            <Col span={12}>
              <Form.Item
                name="dayOfMonth"
                label="Day of month"
                rules={[required()]}
              >
                <InputNumber min={1} max={31} className="w-100" />
              </Form.Item>
            </Col>
          </Row>
        )}

        {is('RepeatInterval') && (
          <Row>
            <Col span={12}>
              <Form.Item
                name="interval"
                label="Interval"
                required
                className="mb-0"
              >
                <Row gutter={DEFAULT_ROW_GUTTER} wrap={false}>
                  <Col flex="none">
                    <Form.Item
                      name="intervalType"
                      requiredMark={'optional'}
                      rules={[required()]}
                    >
                      <Select
                        className="w-100"
                        options={[
                          { value: 'minutes', label: 'Min(s)' },
                          { value: 'hours', label: 'Hour(s)' },
                          { value: 'days', label: 'Days(s)' },
                          { value: 'weeks', label: 'Week(s)' },
                        ]}
                      />
                    </Form.Item>
                  </Col>
                  <Col flex="auto">
                    <Form.Item
                      name="intervalValue"
                      requiredMark={'optional'}
                      rules={[required()]}
                    >
                      <InputNumber min={1} className="w-100" />
                    </Form.Item>
                  </Col>
                </Row>
              </Form.Item>
            </Col>
          </Row>
        )}

        {is('RepeatWeekday', 'RepeatDayOfMonth', 'RepeatInterval') && (
          <Row gutter={DEFAULT_ROW_GUTTER}>
            <Col span={12}>
              <Form.Item
                name="timeOfTheDayUtcMinutes"
                label="Time"
                rules={[required()]}
              >
                <TimePicker
                  showSecond={false}
                  defaultPickerValue={MOMENT_START_OF_DAY}
                  format={TIME_FORMAT}
                  className="w-100"
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="durationMinutes"
                label="Duration (minutes)"
                rules={[required()]}
              >
                <InputNumber min={1} className="w-100" />
              </Form.Item>
            </Col>
          </Row>
        )}
      </Form>
    </Modal>
  );
}
