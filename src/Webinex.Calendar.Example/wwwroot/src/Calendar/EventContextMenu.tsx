import { Event } from '@/http';
import { Dropdown, MenuProps } from 'antd';
import { ItemType } from 'rc-menu/lib/interface';
import { PropsWithChildren, useMemo } from 'react';

export interface EventContextMenuCallbacks {
  onEditTimeClick: (event: Event) => any;
  onCancelAppearanceClick: (event: Event) => any;
  onCancelRepeatClick: (event: Event) => any;
}

export interface EventContextMenuProps extends EventContextMenuCallbacks {
  event: Event;
}

function useMenuItems(props: EventContextMenuProps): ItemType[] {
  const { event, onEditTimeClick, onCancelAppearanceClick, onCancelRepeatClick } = props;

  return useMemo<ItemType[]>(() => {
    return [
      !!event.recurringEventId && {
        key: 'edit-time',
        label: 'Edit time',
        onClick: () => onEditTimeClick(event),
      },
      {
        key: 'cancel',
        label: 'Cancel',
        onClick: () => onCancelAppearanceClick(event),
      },
      {
        key: 'cancel-repeat',
        label: 'Cancel repeat',
        onClick: () => onCancelRepeatClick(event),
      },
    ].filter((x) => !!x) as ItemType[];
  }, [event, onEditTimeClick, onCancelAppearanceClick, onCancelRepeatClick]);
}

const TRIGGER = ['click' as const];

export function EventContextMenu(
  props: PropsWithChildren<EventContextMenuProps>,
) {
  const { children } = props;
  const items = useMenuItems(props);
  const menuProps = useMemo(() => ({ items }), [items]);

  if (items.length === 0) {
    return null;
  }

  return (
    <Dropdown menu={menuProps} placement="bottom" trigger={TRIGGER}>
      {children}
    </Dropdown>
  );
}
