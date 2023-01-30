import { useField } from 'formik';

import { FormsGroup } from './FormsGroup';
import { Select } from 'antd';

export interface FormsSelectOption {
  value: string;
  label: string;
}

export type FormsSelectProps = {
  name: string;
  label?: React.ReactNode;
  required?: boolean;
  options: FormsSelectOption[];
  mode?: 'multiple';
  showSearch?: boolean;
  disabled?: boolean;
  allowClear?: boolean;
  placeholder?: string;
};

export const FormsSelect = ({
  name,
  label,
  required,
  options,
  mode,
  showSearch = true,
  disabled = false,
  allowClear = true,
  placeholder,
}: FormsSelectProps) => {
  const [field, , helpers] = useField(name);

  function handleChange(value: string) {
    helpers.setValue(value || null);
  }

  return (
    <FormsGroup label={label} name={name} required={required}>
      <Select
        mode={mode}
        showSearch={showSearch}
        value={field.value}
        onChange={handleChange}
        onBlur={field.onBlur}
        placeholder={placeholder}
        optionFilterProp="children"
        allowClear={allowClear}
        disabled={disabled}
      >
        {options.map((option) => (
          <Select.Option key={option.value} value={option.value}>
            {option.label}
          </Select.Option>
        ))}
      </Select>
    </FormsGroup>
  );
};
