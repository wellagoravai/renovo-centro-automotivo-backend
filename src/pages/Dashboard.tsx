import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import '../styles/Dashboard.css';

interface ServiceOrder {
  id: string;
  number: string;
  status: string;
  customerName: string;
  vehicleInfo: string;
  entryDate: string;
}

const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [serviceOrders, setServiceOrders] = useState<ServiceOrder[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      const response = await api.get('/service-orders');
      if (response.ok) {
        const data = await response.json();
        setServiceOrders(data);
      }
    } catch (error) {
      console.error('Erro ao carregar dashboard:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColumn = (status: string): string => {
    const statusMap: { [key: string]: string } = {
      'Recebido': 'received',
      'Em Diagnóstico': 'diagnostic',
      'Orçamento': 'budget',
      'Aguardando Aprovação': 'awaiting-approval',
      'Aguardando Peças': 'awaiting-parts',
      'Em Manutenção': 'maintenance',
      'Testes': 'testing',
      'Lavagem': 'washing',
      'Entregue': 'delivered',
    };
    return statusMap[status] || 'received';
  };

  const columns = [
    { id: 'received', title: 'Recebidos', statuses: ['Recebido'] },
    { id: 'diagnostic', title: 'Em Diagnóstico', statuses: ['Em Diagnóstico'] },
    { id: 'budget', title: 'Orçamento', statuses: ['Orçamento'] },
    { id: 'awaiting-approval', title: 'Aguardando Aprovação', statuses: ['Aguardando Aprovação'] },
    { id: 'awaiting-parts', title: 'Aguardando Peças', statuses: ['Aguardando Peças'] },
    { id: 'maintenance', title: 'Em Manutenção', statuses: ['Em Manutenção'] },
    { id: 'testing', title: 'Em Testes', statuses: ['Testes'] },
    { id: 'washing', title: 'Lavagem', statuses: ['Lavagem'] },
    { id: 'delivered', title: 'Pronto para Entrega', statuses: ['Entregue'] },
  ];

  const getOrdersByColumn = (columnId: string) => {
    const column = columns.find(c => c.id === columnId);
    if (!column) return [];
    return serviceOrders.filter(order => column.statuses.indexOf(order.status) >= 0);
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <button
          className="btn-primary btn-new-order"
          onClick={() => navigate('/new-service-order')}
        >
          + Nova Ordem de Serviço
        </button>
      </div>

      <div className="kanban-board">
        {columns.map(column => {
          const orders = getOrdersByColumn(column.id);
          return (
            <div key={column.id} className="kanban-column">
              <div className="column-header">
                <h3>{column.title}</h3>
                <span className="column-count">{orders.length}</span>
              </div>
              <div className="column-content">
                {orders.map(order => (
                  <div
                    key={order.id}
                    className="kanban-card"
                    onClick={() => navigate(`/service-orders/${order.id}`)}
                  >
                    <div className="card-header">
                      <span className="os-number">OS {order.number}</span>
                    </div>
                    <div className="card-body">
                      <p className="vehicle-info">{order.vehicleInfo}</p>
                      <p className="customer-name">{order.customerName}</p>
                    </div>
                    <div className="card-footer">
                      <span className="entry-date">
                        {new Date(order.entryDate).toLocaleDateString('pt-BR')}
                      </span>
                    </div>
                  </div>
                ))}
                {orders.length === 0 && (
                  <div className="empty-column">
                    <p>Nenhuma OS</p>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default Dashboard;