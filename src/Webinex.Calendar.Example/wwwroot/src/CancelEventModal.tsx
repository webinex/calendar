import { Modal } from 'antd';
import moment from 'moment';
import { Event } from './http';
import { DATE_TIME_FORMAT } from './system';

export interface CancelEventModalProps {
  event: Event;
  onConfirm: () => any;
  onCancel: () => any;
}

export function CancelEventModal(props: CancelEventModalProps) {
  const { event, onCancel, onConfirm } = props;
  const isRecurrent = !!event.recurringEventId;

  return (
    <Modal open okText="Cancel" onOk={onConfirm} onCancel={onCancel}>
      Are you sure you want to cancel {isRecurrent ? 'recurrent' : ''} event?
      <br />
      Title: {event.data.title}<br />
      Date: {moment(event.start).format(DATE_TIME_FORMAT)}
    </Modal>
  );
}
