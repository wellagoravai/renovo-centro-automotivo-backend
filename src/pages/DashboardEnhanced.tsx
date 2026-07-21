import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import '../styles/DashboardTV.css';

interface ServiceOrder {
  id: string;
  number: string;
  status: string;
  customerName: string;
  vehicleInfo: string;
  entryDate: string;
  problemReported: string;
}

const DashboardEnhanced: React.FC = () => {
  const navigate = useNavigate();
  const [serviceOrders, setServiceOrders] = useState<ServiceOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [showStatusModal, setShowStatusModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState<ServiceOrder | null>(null);
  const [newStatus, setNewStatus] = useState('');
  const [notes, setNotes] = useState('');
  const [updating, setUpdating] = useState(false);
  const [currentTime, setCurrentTime] = useState(new Date());

  const statusList = [
    'Recebido',
    'Checklist realizado',
    'Em diagnóstico',
    'Orçamento elaborado',
    'Aguardando aprovação',
    'Aprovado',
    'Aguardando peças',
    'Peças recebidas',
    'Em manutenção',
    'Montagem',
    'Testes',
    'Lavagem',
    'Pronto para retirada',
    'Entregue',
    'Cancelado',
  ];

  useEffect(() => {
    loadDashboard();
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
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

  const handleUpdateStatus = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedOrder || !newStatus) return;

    setUpdating(true);
    try {
      const response = await api.patch(`/service-orders/${selectedOrder.id}/status`, {
        status: newStatus,
        notes: notes,
        changedBy: 'Usuário Atual',
      });

      if (response.ok) {
        alert('✅ Status atualizado com sucesso!');
        setShowStatusModal(false);
        setSelectedOrder(null);
        setNewStatus('');
        setNotes('');
        loadDashboard();
      } else {
        const error = await response.json();
        alert(`❌ Erro: ${error.message || 'Erro ao atualizar status'}`);
      }
    } catch (error) {
      console.error('Erro ao atualizar status:', error);
      alert('❌ Erro ao atualizar status');
    } finally {
      setUpdating(false);
    }
  };

  const openStatusModal = (order: ServiceOrder) => {
    setSelectedOrder(order);
    setNewStatus(order.status);
    setNotes('');
    setShowStatusModal(true);
  };

  const getStatusColor = (status: string) => {
    const colors: any = {
      'Recebido': '#3498db',
      'Checklist realizado': '#9b59b6',
      'Em diagnóstico': '#f39c12',
      'Orçamento elaborado': '#1abc9c',
      'Aguardando aprovação': '#e74c3c',
      'Aprovado': '#27ae60',
      'Aguardando peças': '#e67e22',
      'Peças recebidas': '#16a085',
      'Em manutenção': '#3498db',
      'Montagem': '#8e44ad',
      'Testes': '#1abc9c',
      'Lavagem': '#3498db',
      'Pronto para retirada': '#27ae60',
      'Entregue': '#95a5a6',
      'Cancelado': '#e74c3c',
    };
    return colors[status] || '#7f8c8d';
  };

  const getOrdersByStatus = (status: string) => {
    return serviceOrders.filter(order => order.status === status);
  };

  const getMetrics = () => {
    const total = serviceOrders.length;
    const received = getOrdersByStatus('Recebido').length;
    const inProgress = getOrdersByStatus('Em manutenção').length + getOrdersByStatus('Montagem').length + getOrdersByStatus('Testes').length;
    const waiting = getOrdersByStatus('Aguardando aprovação').length + getOrdersByStatus('Aguardando peças').length;
    const ready = getOrdersByStatus('Pronto para retirada').length;
    const delivered = getOrdersByStatus('Entregue').length;
    return { total, received, inProgress, waiting, ready, delivered };
  };

  if (loading) {
    return <div className="loading-tv">Carregando...</div>;
  }

  const metrics = getMetrics();

  return (
    <div className="dashboard-tv">
      {/* Header Principal */}
      <div className="dashboard-tv-header">
        <h1>🔧 RENOVO WORKSHOP - DASHBOARD</h1>
        <div className="dashboard-tv-date">
          {currentTime.toLocaleString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
          })}
        </div>
      </div>

      {/* Métricas Principais */}
      <div className="metrics-container">
        <div className="metric-card total">
          <div className="metric-value">{metrics.total}</div>
          <div className="metric-label">Total OS</div>
        </div>
        <div className="metric-card received">
          <div className="metric-value">{metrics.received}</div>
          <div className="metric-label">Recebidas</div>
        </div>
        <div className="metric-card in-progress">
          <div className="metric-value">{metrics.inProgress}</div>
          <div className="metric-label">Em Manutenção</div>
        </div>
        <div className="metric-card waiting">
          <div className="metric-value">{metrics.waiting}</div>
          <div className="metric-label">Aguardando</div>
        </div>
        <div className="metric-card ready">
          <div className="metric-value">{metrics.ready}</div>
          <div className="metric-label">Prontas</div>
        </div>
        <div className="metric-card delivered">
          <div className="metric-value">{metrics.delivered}</div>
          <div className="metric-label">Entregues</div>
        </div>
      </div>

      {/* Kanban Board TV */}
      <div className="kanban-board-tv">
        {statusList.map(status => {
          const orders = getOrdersByStatus(status);
          return (
            <div key={status} className="kanban-column-tv">
              <div className="column-header-tv">
                <h3>{status}</h3>
                <span className="column-count-tv">{orders.length}</span>
              </div>
              <div className="column-content-tv">
                {orders.map(order => (
                  <div
                    key={order.id}
                    className="kanban-card-tv"
                    onClick={() => navigate(`/service-orders/${order.id}`)}
                  >
                    <div className="card-header-tv">
                      <span className="os-number-tv">OS {order.number}</span>
                      <span
                        className="status-badge-tv"
                        style={{ backgroundColor: getStatusColor(order.status) }}
                      >
                        {order.status}
                      </span>
                    </div>
                    <div className="card-body-tv">
                      <p className="vehicle-info-tv">🚗 {order.vehicleInfo}</p>
                      <p className="customer-name-tv">👤 {order.customerName}</p>
                      <p className="problem-info-tv">🔧 {order.problemReported || 'Não informado'}</p>
                    </div>
                    <div className="card-footer-tv">
                      <span className="entry-date-tv">
                        📅 {new Date(order.entryDate).toLocaleDateString('pt-BR')}
                      </span>
                      <button
                        className="btn-status-update-tv"
                        onClick={(e) => {
                          e.stopPropagation();
                          openStatusModal(order);
                        }}
                      >
                        ✏️ Atualizar
                      </button>
                    </div>
                  </div>
                ))}
                {orders.length === 0 && (
                  <div className="empty-column-tv">
                    <p>Nenhuma OS</p>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Status Update Modal TV */}
      {showStatusModal && selectedOrder && (
        <div className="modal-overlay-tv" onClick={() => setShowStatusModal(false)}>
          <div className="modal-content-tv" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header-tv">
              <h2>Atualizar Status da OS</h2>
              <button className="modal-close-tv" onClick={() => setShowStatusModal(false)}>✕</button>
            </div>
            <form onSubmit={handleUpdateStatus}>
              <div className="form-group-tv">
                <label>Ordem de Serviço:</label>
                <input
                  type="text"
                  value={`OS ${selectedOrder.number}`}
                  disabled
                  className="form-control-tv"
                />
              </div>
              <div className="form-group-tv">
                <label>Cliente:</label>
                <input
                  type="text"
                  value={selectedOrder.customerName}
                  disabled
                  className="form-control-tv"
                />
              </div>
              <div className="form-group-tv">
                <label>Status Atual:</label>
                <input
                  type="text"
                  value={selectedOrder.status}
                  disabled
                  className="form-control-tv"
                />
              </div>
              <div className="form-group-tv">
                <label>Novo Status *</label>
                <select
                  value={newStatus}
                  onChange={(e) => setNewStatus(e.target.value)}
                  className="form-control-tv"
                  required
                >
                  {statusList.map(status => (
                    <option key={status} value={status}>{status}</option>
                  ))}
                </select>
              </div>
              <div className="form-group-tv">
                <label>Observações:</label>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Adicione observações sobre a mudança de status..."
                  rows={3}
                  className="form-control-tv"
                />
              </div>
              <div className="modal-actions-tv">
                <button
                  type="button"
                  className="btn-tv btn-secondary-tv"
                  onClick={() => setShowStatusModal(false)}
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="btn-tv btn-primary-tv"
                  disabled={updating}
                >
                  {updating ? '⏳ Atualizando...' : '✅ Atualizar Status'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default DashboardEnhanced;
