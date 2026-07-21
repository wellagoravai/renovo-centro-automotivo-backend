import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api } from '../services/api';
import '../styles/PurchaseOrders.css';

interface PurchaseOrder {
  id: string;
  number: string;
  supplierName: string;
  status: string;
  createdAt: string;
  expectedDeliveryDate?: string;
  notes: string;
  items: any[];
}

const PurchaseOrders: React.FC = () => {
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const { hasPermission } = useAuth();

  useEffect(() => {
    loadOrders();
  }, [statusFilter]);

  const loadOrders = async () => {
    try {
      const url = statusFilter ? `/PurchaseOrders?status=${statusFilter}` : '/PurchaseOrders';
      const response = await api.get(url);
      const data = await response.json();
      setOrders(data);
    } catch (error) {
      console.error('Erro ao carregar pedidos:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    const colors: any = {
      'Pendente': '#f39c12',
      'Aprovado': '#27ae60',
      'Enviado': '#3498db',
      'Recebido': '#1abc9c',
      'Cancelado': '#e74c3c'
    };
    return colors[status] || '#7f8c8d';
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="purchase-orders-page">
      <div className="page-header">
        <h1>Pedidos de Compra</h1>
        {hasPermission('inventory.write') && (
          <button className="btn btn-primary" onClick={() => alert('Funcionalidade em desenvolvimento')}>
            + Novo Pedido
          </button>
        )}
      </div>

      <div className="filters-bar">
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="form-control filter-select"
        >
          <option value="">Todos os Status</option>
          <option value="Pendente">Pendente</option>
          <option value="Aprovado">Aprovado</option>
          <option value="Enviado">Enviado</option>
          <option value="Recebido">Recebido</option>
          <option value="Cancelado">Cancelado</option>
        </select>
      </div>

      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Número</th>
              <th>Fornecedor</th>
              <th>Status</th>
              <th>Data Criação</th>
              <th>Previsão Entrega</th>
              <th>Itens</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(order => (
              <tr key={order.id}>
                <td><strong>{order.number}</strong></td>
                <td>{order.supplierName}</td>
                <td>
                  <span className="badge" style={{ backgroundColor: getStatusColor(order.status) }}>
                    {order.status}
                  </span>
                </td>
                <td>{new Date(order.createdAt).toLocaleDateString('pt-BR')}</td>
                <td>
                  {order.expectedDeliveryDate 
                    ? new Date(order.expectedDeliveryDate).toLocaleDateString('pt-BR')
                    : '-'
                  }
                </td>
                <td>{order.items?.length || 0}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {orders.length === 0 && (
        <div className="empty-state">
          <p>Nenhum pedido de compra encontrado</p>
        </div>
      )}
    </div>
  );
};

export default PurchaseOrders;