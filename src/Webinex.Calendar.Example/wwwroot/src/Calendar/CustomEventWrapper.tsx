import { CSSProperties, useMemo } from 'react';
import { Col, Popover, Row, Space } from 'antd';
import classNames from 'classnames';
import { EventWrapperProps } from 'react-big-calendar';
import { FORMATS, calendarLocalizaer } from './calendarLocalizer';
import { CalendarEvent } from './calendarTypes';
import styles from './Calendar.module.scss';
import { EllipsisOutlined } from '@ant-design/icons';
import {
  EventContextMenu,
  EventContextMenuCallbacks,
} from './EventContextMenu';

function useStyles(props: EventWrapperProps<CalendarEvent>) {
  const { style } = props;

  return useMemo<CSSProperties | undefined>(
    () =>
      style
        ? {
            top: style!.top + '%',
            height: style!.height + '%',
            '--x-offset': style!.xOffset,
          }
        : undefined,
    [style],
  );
}

function CustomEventWrapper(
  props: EventWrapperProps<CalendarEvent> & EventContextMenuCallbacks,
) {
  const {
    event,
    style,
    onEditTimeClick,
    onCancelAppearanceClick,
    onCancelRepeatClick,
  } = props;
  const eventStyles = useStyles(props);

  const { data } = event.original;
  const { title } = data;

  return (
    <div
      className={classNames(styles.event, {
        [styles.time]: !!style,
      })}
      style={eventStyles}
      tabIndex={0}
    >
      <div className={styles.content}>
        <Row wrap={false}>
          <Col flex="auto">
            <Space direction="vertical" className={styles.title}>
              <span>{title}</span>
            </Space>
            <div className={styles.time}>
              {calendarLocalizaer.format(event.start!, FORMATS.eventTime)} -{' '}
              {calendarLocalizaer.format(event.end!, FORMATS.eventTime)}
            </div>
          </Col>
          <Col flex="none" className={styles.moreIcon}>
            <EventContextMenu
              event={event.original}
              onEditTimeClick={onEditTimeClick}
              onCancelAppearanceClick={onCancelAppearanceClick}
              onCancelRepeatClick={onCancelRepeatClick}
            >
              {<EllipsisOutlined />}
            </EventContextMenu>
          </Col>
        </Row>
      </div>
    </div>
  );
}

export function createCustomEventWrapper(
  callbacks: EventContextMenuCallbacks,
): React.ComponentType<EventWrapperProps<CalendarEvent>> {
  return (props: EventWrapperProps<CalendarEvent>) => (
    <CustomEventWrapper {...props} {...callbacks} />
  );
}
