import { Col, DatePicker, Form, Modal, Row } from 'antd';
import { FormInstance } from 'antd/es/form/Form';
import moment, { Moment } from 'moment';
import { useCallback } from 'react';
import { required, rule } from './rule';
import { DATE_TIME_FORMAT, MOMENT_START_OF_DAY } from './system';

export interface EditEventTimeModalProps {
  initialValues: EditEventTimeFormValue;
  onCancel: () => any;
  onSubmit: (value: EditEventTimeFormValue) => any;
}

export interface EditEventTimeFormValue {
  start: Moment;
  end: Moment;
}

function useSubmit(
  props: EditEventTimeModalProps,
  form: FormInstance<EditEventTimeFormValue>,
) {
  const { onSubmit } = props;
  return useCallback(
    () =>
      form
        .validateFields()
        .then((value) =>
          onSubmit({
            ...value,
            start: moment(value.start).startOf('minute'),
            end: moment(value.end).startOf('minute'),
          }),
        ),
    [form],
  );
}

export function EditEventTimeModal(props: EditEventTimeModalProps) {
  const { onCancel, initialValues } = props;
  const [form] = Form.useForm<EditEventTimeFormValue>();
  const handleSubmit = useSubmit(props, form);
  const start = Form.useWatch('start', form);

  return (
    <Modal
      open
      title="Edit event time"
      okText="Edit"
      onOk={handleSubmit}
      onCancel={onCancel}
    >
      <Form
        name="edit-event-time"
        form={form}
        initialValues={initialValues}
        layout="vertical"
      >
        <Row gutter={20} wrap={false}>
          <Col span={12}>
            <Form.Item name="start" label="Start" rules={[required()]}>
              <DatePicker
                showTime={{ defaultValue: MOMENT_START_OF_DAY }}
                showSecond={false}
                format={DATE_TIME_FORMAT}
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
                required(),
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
                showTime={{ defaultValue: MOMENT_START_OF_DAY }}
                showSecond={false}
                format={DATE_TIME_FORMAT}
              />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
}
