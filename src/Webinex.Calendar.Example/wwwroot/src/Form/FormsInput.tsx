import { useField } from 'formik';
import { FormsGroup } from './FormsGroup';
import React from 'react';
import { Input, InputProps } from 'antd';

export type FormsInputProps = {
  name: string;
  label?: React.ReactNode;
  type?: InputProps['type'];
  required?: boolean;
  disabled?: boolean;
  className?: string;
  placeholder?: string;
};

export const FormsInput = ({
  name,
  type,
  label,
  required,
  disabled,
  className,
  placeholder,
}: FormsInputProps) => {
  const [field, , helpers] = useField(name);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const value = e.target.value;

    if (value?.length === 0) {
      helpers.setValue(null);
    } else {
      field.onChange(e);
    }
  }

  return (
    <FormsGroup label={label} name={name} required={required}>
      <Input
        type={type}
        {...field}
        value={field.value ?? ''}
        onChange={handleChange}
        className={className}
        disabled={disabled}
        placeholder={placeholder}
      />
    </FormsGroup>
  );
};
