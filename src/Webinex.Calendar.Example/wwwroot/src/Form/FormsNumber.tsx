import { useField } from 'formik';
import { FormsGroup } from './FormsGroup';
import React from 'react';
import { InputNumber, InputNumberProps } from 'antd';
import { useCallback } from 'react';
import classnames from 'classnames';

export type FormsNumberProps = {
  name: string;
  label?: React.ReactNode | false;
  required?: boolean;
  className?: string;
  placeholder?: string;
  prefix?: React.ReactNode;
  min?: number;
  max?: number;
  precision?: number;
  onBlur?: InputNumberProps['onBlur'];
};

export const defaultInputNumberProps: InputNumberProps<number> = {
  formatter: (value?: number) => getThousandSeparatorExp(value),
  parser: (value?: string) => parseFormattedValue(value),
  className: 'w-100',
};

function getThousandSeparatorExp(value: number | undefined): string {
  return `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

function parseFormattedValue(value: string | undefined): number {
  return +value!.replace(/\$\s?|(,*)/g, '');
}

export const FormsNumber = ({
  name,
  label,
  required,
  className,
  placeholder,
  prefix,
  min,
  max,
  precision = 2,
  onBlur,
}: FormsNumberProps) => {
  const [field, , { setValue, setTouched }] = useField(name);

  const handleChange = useCallback(
    (value: number | null) => {
      setTouched(true);
      setValue(value, true);
    },
    [setValue],
  );

  const { onBlur: fieldOnBlur } = field;

  const handleBlur = useCallback(
    (e: React.FocusEvent<HTMLInputElement>) => {
      fieldOnBlur(e);
      onBlur && onBlur(e);
    },
    [onBlur, fieldOnBlur],
  );

  return (
    <FormsGroup label={label} name={name} required={required}>
      <InputNumber
        {...field}
        {...defaultInputNumberProps}
        onBlur={handleBlur}
        prefix={prefix}
        value={field.value ?? ''}
        onChange={handleChange}
        className={classnames(defaultInputNumberProps?.className, className)}
        placeholder={placeholder}
        min={min}
        max={max}
        precision={precision}
        style={{ width: '100%' }}
      />
    </FormsGroup>
  );
};
