import React from 'react';
import './ProductCard.tsx';

interface Product {
  id: string;
  name: string;
  category: string;
  description: string;
  price?: number;
}

interface ProductCardProps {
  product: Product;
  onSelect?: (product: Product) => void;
}

const ProductCard: React.FC<ProductCardProps> = ({ product, onSelect }) => {
  return (
    <div className="product-card">
      <div className="product-image">
        <img src="/assets/product-placeholder.png" alt={product.name} />
      </div>
      <div className="product-info">
        <h3>{product.name}</h3>
        <p className="category">{product.category}</p>
        <p className="description">{product.description}</p>
        {product.price && <p className="price">R$ {product.price.toFixed(2)}</p>}
        <button className="button" onClick={() => onSelect && onSelect(product)}>
          Mais Informações
        </button>
      </div>
    </div>
  );
};

export default ProductCard;
