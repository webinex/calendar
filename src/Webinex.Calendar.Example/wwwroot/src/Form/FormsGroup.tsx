import React from 'react';
import { Form } from 'antd';
import classNames from 'classnames';
import { useFormErrorMessage } from './FormsErrorMessage';

export interface FormsGroupProps {
  name: string;
  label?: React.ReactNode;
  required?: boolean;
  children: React.ReactNode;
  inline?: boolean;
}

export const FormsGroup = ({
  children,
  name,
  label,
  required,
  inline,
}: FormsGroupProps) => {
  const error = useFormErrorMessage({ name, label });

  return (
    <Form.Item
      label={label}
      required={required}
      colon={false}
      validateStatus={error.show ? 'error' : ''}
      help={error.show && error.error}
      className={classNames({ 'form-item-vertical': !inline }, 'form-group')}
    >
      {children}
    </Form.Item>
  );
};
