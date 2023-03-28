import { Rule } from 'antd/lib/form';

export function rule(
  predicate: (value: any) => boolean,
  errorMessage: string,
): Rule {
  return {
    validator: (_, value) => {
      return predicate(value) ? Promise.resolve() : Promise.reject(new Error(errorMessage));
    },
  }
}

export function required(isRequired: boolean = true): Rule {
  return {
    required: isRequired,
    message: "This field is required",
  }
}
