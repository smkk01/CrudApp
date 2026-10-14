export type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  category: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type ProductInput = {
  name: string;
  description: string;
  price: number;
  category: string;
  isActive: boolean;
};
