import { useField } from 'formik';
import { FormsGroup } from './FormsGroup';
import { DatePicker, DatePickerProps } from 'antd';
import moment from 'moment';
import { useCallback, useMemo } from 'react';

export type FormsDateProps = {
  name: string;
  label?: React.ReactNode;
  required?: boolean;
  className?: string;
  placeholder?: string;
  disabled?: boolean;
  allowClear?: boolean;
  showTime?: boolean;
};

export const FormsDate = ({
  name,
  label,
  required,
  className,
  placeholder,
  disabled,
  allowClear,
  showTime,
}: FormsDateProps) => {
  const [field, , { setValue, setTouched }] = useField({
    name,
    type: 'date',
  });

  const handleChange = useCallback(
    (value: DatePickerProps['value']) => {
      setTouched(true);
      value = showTime ? value?.startOf('second') : value?.startOf('day');
      setValue(value, true);
    },
    [setValue, setTouched, showTime],
  );

  const value = useMemo(() => {
    return typeof field.value === 'string'
      ? moment(field.value)
      : field.value ?? '';
  }, [field.value]);

  return (
    <FormsGroup label={label} name={name} required={required}>
      <DatePicker
        {...field}
        onChange={handleChange}
        className={className}
        placeholder={placeholder}
        disabled={disabled}
        value={value}
        style={{ width: '100%' }}
        allowClear={allowClear}
        showTime={
          showTime
            ? {
                defaultValue: moment().startOf('day'),
              }
            : undefined
        }
      />
    </FormsGroup>
  );
};
