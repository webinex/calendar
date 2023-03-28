import { Button, Col, Menu, Row, Space } from 'antd';
import { LeftOutlined, RightOutlined } from '@ant-design/icons';
import { Navigate, ToolbarProps, View, Event, ViewsProps } from 'react-big-calendar';
import styles from './Calendar.module.scss';
import { useCallback, useMemo } from 'react';
import { SelectInfo } from 'rc-menu/lib/interface';
import { isArray } from 'lodash';
import { ItemType } from 'antd/lib/menu/hooks/useItems';

function isViewDefined(views: ViewsProps<Event>, view: View) {
  return isArray(views) ? views.includes(view) : !!views[view];
}

function Title(props: ToolbarProps<Event, object>) {
  const { label, onNavigate } = props;
  const gotoNext = useCallback(() => onNavigate(Navigate.NEXT), [onNavigate]);
  const gotoPrev = useCallback(() => onNavigate(Navigate.PREVIOUS), [onNavigate]);

  return (
    <Space className={styles.range} size="large">
      <Button
        className={styles.navigation}
        type="link"
        onClick={gotoPrev}
        icon={<LeftOutlined />}
      />
      <span className={styles.label}>{label}</span>
      <Button
        className={styles.navigation}
        type="link"
        onClick={gotoNext}
        icon={<RightOutlined />}
      />
    </Space>
  );
}

function ViewsMenu(props: ToolbarProps<Event, object>) {
  const { onView, view, views } = props;
  const onSelect = useCallback((value: SelectInfo) => onView(value.selectedKeys.at(0)! as View), [onView]);
  const value = useMemo(() => [view], [view]);
  const items = useMemo<ItemType[]>(
    () =>
      [
        { key: 'day' as const, label: "Day" },
        { key: 'week' as const, label: "Week" },
        { key: 'month' as const, label: "Month" },
      ].filter((item) => isViewDefined(views, item.key)),
    [views],
  );

  return (
    <Menu
      rootClassName={styles.views}
      selectedKeys={value}
      onSelect={onSelect}
      className="nav"
      mode="horizontal"
      disabledOverflow
      items={items}
    />
  );
}

export function CustomToolbar(props: ToolbarProps<Event, object>) {
  const { onNavigate } = props;
  const gotoToday = useCallback(() => onNavigate(Navigate.TODAY), [onNavigate]);

  return (
    <Row className={styles.toolbar} align="middle" justify="space-between">
      <Col>
        <div className={styles.actions}>
          <Button className={styles.today} onClick={gotoToday} type="link">
            Today
          </Button>
        </div>
      </Col>
      <Col>
        <Title {...props} />
      </Col>
      <Col>
        <ViewsMenu {...props} />
      </Col>
    </Row>
  );
}
