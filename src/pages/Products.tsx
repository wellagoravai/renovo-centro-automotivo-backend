import React from 'react';
import './Products.css';

const Products: React.FC = () => {
    const products = [
        {
            id: '1',
            name: 'Óleo Sintético Premium',
            category: 'Fluidos',
            description: 'Óleo de motor sintético de alta performance para proteção máxima do motor.',
        },
        {
            id: '2',
            name: 'Filtro de Ar',
            category: 'Filtros',
            description: 'Filtro de ar de reposição para melhor fluxo de ar no motor.',
        },
        {
            id: '3',
            name: 'Pastilhas de Freio',
            category: 'Freios',
            description: 'Pastilhas de freio de cerâmica para segurança e durabilidade.',
        },
        {
            id: '4',
            name: 'Velas de Ignição',
            category: 'Ignição',
            description: 'Velas de ignição de qualidade para melhor desempenho do motor.',
        },
    ];

    return (
        <div className="products-page">
            <section className="products-header">
                <h1>Nossos Produtos</h1>
                <p>Conheça nossa seleção de peças e produtos de qualidade para sua manutenção automotiva.</p>
            </section>

            <section className="products-list">
                <div className="container">
                    <div className="products-grid">
                        {products.map((product) => (
                            <div key={product.id} className="product-item">
                                <div className="product-image">
                                    <img src="/assets/product-placeholder.png" alt={product.name} />
                                </div>
                                <div className="product-info">
                                    <h3>{product.name}</h3>
                                    <p className="category">{product.category}</p>
                                    <p className="description">{product.description}</p>
                                    <button className="button">Consultar Preço</button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            <section className="products-cta">
                <h2>Não encontrou o que procura?</h2>
                <p>Entre em contato conosco e solicite um orçamento personalizado!</p>
                <button className="button">📞 Fale Conosco</button>
            </section>
        </div>
    );
};

export default Products;
