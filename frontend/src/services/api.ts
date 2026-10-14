import type { Product, ProductInput } from '../types/product';
import { getIdToken } from '../auth/cognito';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5155';

async function authorizedFetch(url: string, init: RequestInit = {}): Promise<Response> {
  const token = await getIdToken();
  const headers = new Headers(init.headers);
  headers.set('Authorization', token);
  return fetch(url, { ...init, headers });
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || 'Request failed');
  }

  return (await response.json()) as T;
}

export const api = {
  getProducts: async (): Promise<Product[]> => {
    const response = await authorizedFetch(`${API_BASE_URL}/api/products`);
    return handleResponse<Product[]>(response);
  },

  createProduct: async (payload: ProductInput): Promise<Product> => {
    const response = await authorizedFetch(`${API_BASE_URL}/api/products`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    return handleResponse<Product>(response);
  },

  updateProduct: async ({ id, payload }: { id: string; payload: ProductInput }): Promise<Product> => {
    const response = await authorizedFetch(`${API_BASE_URL}/api/products/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    return handleResponse<Product>(response);
  },

  deleteProduct: async (id: string): Promise<void> => {
    const response = await authorizedFetch(`${API_BASE_URL}/api/products/${id}`, {
      method: 'DELETE'
    });

    if (!response.ok) {
      throw new Error('Unable to delete product');
    }
  }
};
