import { Col, DatePicker, Form, Modal, Row } from 'antd';
import moment, { Moment } from 'moment';
import { useCallback } from 'react';
import { required, rule } from './rule';
import { DATE_FORMAT } from './system';

export interface CancelRecurrentEventModalProps {
  eventTitle: string;
  onSubmit: (value: CancelRecurrentEventFormValue) => any;
  onCancel: () => any;
}

export interface CancelRecurrentEventFormValue {
  since: Moment;
}

const INITIAL_VALUE: CancelRecurrentEventFormValue = {
  since: moment().add(1, 'day').startOf('day'),
};

export function CancelRecurrentEventModal(
  props: CancelRecurrentEventModalProps,
) {
  const { eventTitle, onSubmit, onCancel } = props;
  const [form] = Form.useForm<CancelRecurrentEventFormValue>();
  const handleOk = useCallback(
    () => form.validateFields().then((value) => onSubmit(value)),
    [onSubmit, form],
  );

  return (
    <Modal
      open
      title="Cancel recurrent event"
      okText="Cancel"
      onCancel={onCancel}
      onOk={handleOk}
    >
      <Form
        name="create-event"
        form={form}
        initialValues={INITIAL_VALUE}
        layout="vertical"
      >
        <Row gutter={20} wrap={false}>
          <Col span={12}>
            <Form.Item
              name="since"
              label="Since"
              rules={[
                required(),
                rule((value) =>
                  moment(value).isSameOrAfter(INITIAL_VALUE.since),
                  'Might be at least tomorrow date'
                ),
              ]}
            >
              <DatePicker
                showTime={false}
                showSecond={false}
                format={DATE_FORMAT}
                className="w-100"
              />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
}
