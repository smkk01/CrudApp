import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../services/api';
import type { ProductInput } from '../types/product';

export function useProducts() {
  return useQuery({
    queryKey: ['products'],
    queryFn: api.getProducts
  });
}

export function useProductMutations() {
  const queryClient = useQueryClient();

  const createProduct = useMutation({
    mutationFn: api.createProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    }
  });

  const updateProduct = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ProductInput }) => api.updateProduct({ id, payload }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    }
  });

  const deleteProduct = useMutation({
    mutationFn: api.deleteProduct,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    }
  });

  return { createProduct, updateProduct, deleteProduct };
}
