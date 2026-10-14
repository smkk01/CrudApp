import { zodResolver } from '@hookform/resolvers/zod';
import {
  Button,
  FormControlLabel,
  Stack,
  Switch,
  TextField
} from '@mui/material';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

const productSchema = z.object({
  name: z.string().trim().min(2, 'Name must contain at least 2 characters.'),
  description: z.string().trim().min(10, 'Description must contain at least 10 characters.'),
  price: z.coerce.number().positive('Price must be greater than zero.'),
  category: z.string().trim().min(2, 'Category is required.'),
  isActive: z.boolean()
});

export type ProductFormValues = z.infer<typeof productSchema>;

const emptyValues: ProductFormValues = {
  name: '',
  description: '',
  price: 0,
  category: '',
  isActive: true
};

type ProductFormProps = {
  onSubmit: (values: ProductFormValues) => void | Promise<void>;
  submitLabel: string;
  defaultValues?: Partial<ProductFormValues>;
};

export function ProductForm({ onSubmit, submitLabel, defaultValues }: ProductFormProps) {
  const form = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: { ...emptyValues, ...defaultValues }
  });

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} noValidate>
      <Stack spacing={2}>
        <TextField
          label="Product name"
          {...form.register('name')}
          error={Boolean(form.formState.errors.name)}
          helperText={form.formState.errors.name?.message ?? ' '}
        />

        <TextField
          label="Description"
          multiline
          minRows={3}
          {...form.register('description')}
          error={Boolean(form.formState.errors.description)}
          helperText={form.formState.errors.description?.message ?? ' '}
        />

        <TextField
          label="Price"
          type="number"
          slotProps={{ htmlInput: { step: '0.01', min: '0' } }}
          {...form.register('price')}
          error={Boolean(form.formState.errors.price)}
          helperText={form.formState.errors.price?.message ?? ' '}
        />

        <TextField
          label="Category"
          {...form.register('category')}
          error={Boolean(form.formState.errors.category)}
          helperText={form.formState.errors.category?.message ?? ' '}
        />

        <FormControlLabel
          control={
            <Switch
              checked={Boolean(form.watch('isActive'))}
              onChange={(event) => form.setValue('isActive', event.target.checked, { shouldDirty: true })}
            />
          }
          label="Active product"
        />

        <Button type="submit" variant="contained" size="large">
          {submitLabel}
        </Button>
      </Stack>
    </form>
  );
}
