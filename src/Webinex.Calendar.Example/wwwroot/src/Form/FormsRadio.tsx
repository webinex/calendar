import { useField } from 'formik';
import { Radio, RadioChangeEvent, Space, SpaceProps } from 'antd';
import { FormsGroup } from './FormsGroup';
import { useCallback } from 'react';
import { isEmpty } from 'lodash';
import { RadioGroupButtonStyle, RadioGroupOptionType } from 'antd/lib/radio';

export interface FormRadioOption<T extends string | boolean | number> {
  value: T;
  label: React.ReactNode;
}

export type FormsRadioProps<T extends string | boolean | number> = {
  name: string;
  label?: React.ReactNode;
  required?: boolean;
  className?: string;
  options: FormRadioOption<T>[];
  inline?: boolean;
  optionType?: RadioGroupOptionType;
  buttonStyle?: RadioGroupButtonStyle;
  direction?: SpaceProps['direction'];
};

function useHandleChange<T extends string | boolean | number>(
  props: FormsRadioProps<T>,
) {
  const { name, options } = props;
  const [, , { setValue }] = useField<T>({ name });

  return useCallback(
    (e: RadioChangeEvent) => {
      const value: string = e.target.value.toString();

      if (isEmpty(value)) {
        setValue(null!);
        return;
      }

      const option = options.find((x) => x.value.toString() === value)!;
      setValue(option.value);
    },
    [options, setValue],
  );
}

export function FormsRadio<T extends string | boolean | number>(
  props: FormsRadioProps<T>,
) {
  const {
    name,
    label,
    required,
    className,
    options,
    inline = true,
    optionType,
    buttonStyle,
    direction,
  } = props;

  const [{ value: checked }] = useField<T>({ name });
  const handleChange = useHandleChange(props);
  const optionsElement = (
    <>
      {options.map(({ value, label }) => (
        <Radio
          key={value.toString()}
          checked={checked?.toString() === value.toString()}
          value={value.toString()}
        >
          {label}
        </Radio>
      ))}
    </>
  );

  return (
    <FormsGroup label={label} name={name} required={required} inline={inline}>
      <Radio.Group
        optionType={optionType}
        buttonStyle={buttonStyle}
        value={checked?.toString() ?? ''}
        onChange={handleChange}
        className={className}
      >
        {optionType !== 'button' && (
          <Space direction={direction}>{optionsElement}</Space>
        )}
        {optionType === 'button' && optionsElement}
      </Radio.Group>
    </FormsGroup>
  );
}
