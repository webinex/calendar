import { useField } from 'formik';

export interface UseFormErrorMessageArgs {
  name: string;
  label?: React.ReactNode;
  mode?: 'touched' | 'always';
}

export type FormsErrorMessageProps = UseFormErrorMessageArgs & {
  children?: (error: string) => JSX.Element;
};

export function useFormErrorMessage({
  name,
  mode = 'touched',
}: UseFormErrorMessageArgs) {
  const [, { error, touched }] = useField(name);
  const show = error && (mode !== 'touched' || touched);

  return {
    error,
    show,
  };
}

export function FormsErrorMessage(props: FormsErrorMessageProps) {
  const { children } = props;
  const { error, show } = useFormErrorMessage(props);

  if (!show) {
    return null;
  }

  if (children) {
    return children(error ?? '');
  }

  return <span>{error}</span>;
}
