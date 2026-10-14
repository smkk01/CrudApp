import { Add, Delete, Edit, Inventory2, TrendingUp } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  Typography
} from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { ProductForm, type ProductFormValues } from '../components/ProductForm';
import { useProductMutations, useProducts } from '../hooks/useProducts';
import type { Product } from '../types/product';

export function DashboardPage() {
  const { data: products = [], isLoading, isError, error } = useProducts();
  const { createProduct, updateProduct, deleteProduct } = useProductMutations();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);

  const summary = useMemo(() => {
    const totalValue = products.reduce((sum, product) => sum + product.price, 0);
    const activeCount = products.filter((product) => product.isActive).length;
    const categories = new Set(products.map((product) => product.category)).size;

    return { totalValue, activeCount, categories };
  }, [products]);

  const columns: GridColDef<Product>[] = [
    { field: 'name', headerName: 'Name', flex: 1.5, minWidth: 180 },
    { field: 'category', headerName: 'Category', flex: 1, minWidth: 130 },
    {
      field: 'price',
      headerName: 'Price',
      flex: 0.8,
      minWidth: 120,
      valueFormatter: (value) => {
        const price = Number(value);
        return new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD'
        }).format(price);
      }
    },
    {
      field: 'isActive',
      headerName: 'Status',
      flex: 0.8,
      minWidth: 120,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'default'}
          size="small"
          variant={params.value ? 'filled' : 'outlined'}
        />
      )
    },
    {
      field: 'updatedAt',
      headerName: 'Updated',
      flex: 1,
      minWidth: 170,
      valueFormatter: (value) => new Date(value as string).toLocaleDateString()
    },
    {
      field: 'actions',
      headerName: 'Actions',
      sortable: false,
      width: 180,
      renderCell: (params) => (
        <Stack direction="row" spacing={1}>
          <Button size="small" color="primary" startIcon={<Edit />} onClick={() => handleEdit(params.row)}>
            Edit
          </Button>
          <Button size="small" color="error" startIcon={<Delete />} onClick={() => handleDelete(params.row.id)}>
            Delete
          </Button>
        </Stack>
      )
    }
  ];

  const handleSubmit = async (values: ProductFormValues) => {
    if (editingProduct) {
      await updateProduct.mutateAsync({ id: editingProduct.id, payload: values });
    } else {
      await createProduct.mutateAsync(values);
    }

    setDialogOpen(false);
    setEditingProduct(null);
  };

  const handleEdit = (product: Product) => {
    setEditingProduct(product);
    setDialogOpen(true);
  };

  const handleDelete = async (productId: string) => {
    await deleteProduct.mutateAsync(productId);
  };

  return (
    <Box sx={{ display: 'grid', gap: 3 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', alignItems: { xs: 'flex-start', md: 'center' } }}>
        <Box>
          <Typography variant="overline" sx={{ color: '#475569', letterSpacing: '0.12em' }}>
            Product control center
          </Typography>
          <Typography variant="h4">Warehouse operations dashboard</Typography>
        </Box>

        <Button variant="contained" startIcon={<Add />} onClick={() => {
          setEditingProduct(null);
          setDialogOpen(true);
        }}>
          Add product
        </Button>
      </Stack>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="subtitle2" color="text.secondary">Inventory value</Typography>
                <TrendingUp color="primary" />
              </Stack>
              <Typography variant="h4" sx={{ mt: 1 }}>
                {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(summary.totalValue)}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="subtitle2" color="text.secondary">Active products</Typography>
                <Inventory2 color="success" />
              </Stack>
              <Typography variant="h4" sx={{ mt: 1 }}>
                {summary.activeCount}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Card>
            <CardContent>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="subtitle2" color="text.secondary">Categories</Typography>
                <Chip label={`${summary.categories}`} color="secondary" />
              </Stack>
              <Typography variant="h4" sx={{ mt: 1 }}>
                {summary.categories}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {isError ? (
        <Alert severity="error">{error instanceof Error ? error.message : 'Unable to load products.'}</Alert>
      ) : null}

      <Card>
        <CardContent sx={{ height: 520 }}>
          <DataGrid
            rows={products}
            columns={columns}
            loading={isLoading}
            disableRowSelectionOnClick
            pageSizeOptions={[5, 10, 20]}
            initialState={{ pagination: { paginationModel: { page: 0, pageSize: 5 } } }}
            sx={{ border: 0 }}
          />
        </CardContent>
      </Card>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editingProduct ? 'Edit product' : 'Create product'}</DialogTitle>
        <DialogContent>
          <ProductForm
            onSubmit={handleSubmit}
            submitLabel={editingProduct ? 'Update product' : 'Create product'}
            defaultValues={editingProduct ? {
              name: editingProduct.name,
              description: editingProduct.description,
              price: editingProduct.price,
              category: editingProduct.category,
              isActive: editingProduct.isActive
            } : undefined}
          />
        </DialogContent>
      </Dialog>
    </Box>
  );
}
