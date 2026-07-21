import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface ServiceOrder {
  id: string;
  number: string;
  status: string;
  customerName: string;
  vehicleInfo: string;
  problemReported: string;
  diagnosis?: string;
  services?: string;
  parts?: string;
  estimatedValue?: number;
  entryDate: string;
  estimatedDate?: string;
  finalDate?: string;
}

const ServiceOrderDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('dados');
  const [serviceOrder, setServiceOrder] = useState<ServiceOrder | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadServiceOrder();
  }, [id]);

  const loadServiceOrder = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch(`/api/service-orders/${id}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setServiceOrder(data);
      }
    } catch (error) {
      console.error('Erro ao carregar OS:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    const colors: { [key: string]: string } = {
      'Recebido': '#3498db',
      'Em Diagnóstico': '#9b59b6',
      'Orçamento': '#f39c12',
      'Aguardando Aprovação': '#e67e22',
      'Aguardando Peças': '#e74c3c',
      'Em Manutenção': '#1abc9c',
      'Testes': '#2ecc71',
      'Lavagem': '#16a085',
      'Entregue': '#27ae60',
    };
    return colors[status] || '#95a5a6';
  };

  const tabs = [
    { id: 'dados', label: 'Dados' },
    { id: 'diagnostico', label: 'Diagnóstico' },
    { id: 'checklist', label: 'Checklist' },
    { id: 'fotos', label: 'Fotos' },
    { id: 'historico', label: 'Histórico' },
  ];

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  if (!serviceOrder) {
    return <div className="error">Ordem de serviço não encontrada</div>;
  }

  return (
    <div className="service-order-details">
      <div className="os-header">
        <div className="os-title">
          <h1>OS {serviceOrder.number}</h1>
          <span
            className="status-badge"
            style={{ backgroundColor: getStatusColor(serviceOrder.status) }}
          >
            {serviceOrder.status}
          </span>
        </div>
        <button className="btn-back" onClick={() => navigate('/service-orders')}>
          ← Voltar
        </button>
      </div>

      <div className="os-info-cards">
        <div className="info-card">
          <h3>Cliente</h3>
          <p>{serviceOrder.customerName}</p>
        </div>
        <div className="info-card">
          <h3>Veículo</h3>
          <p>{serviceOrder.vehicleInfo}</p>
        </div>
        <div className="info-card">
          <h3>Entrada</h3>
          <p>{new Date(serviceOrder.entryDate).toLocaleString('pt-BR')}</p>
        </div>
      </div>

      <div className="tabs-container">
        <div className="tabs-header">
          {tabs.map(tab => (
            <button
              key={tab.id}
              className={`tab-button ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="tab-content">
          {activeTab === 'dados' && (
            <div className="tab-pane">
              <h3>Dados da Ordem de Serviço</h3>
              <div className="data-grid">
                <div className="data-item">
                  <label>Número da OS:</label>
                  <span>{serviceOrder.number}</span>
                </div>
                <div className="data-item">
                  <label>Status:</label>
                  <span>{serviceOrder.status}</span>
                </div>
                <div className="data-item">
                  <label>Data de Entrada:</label>
                  <span>{new Date(serviceOrder.entryDate).toLocaleString('pt-BR')}</span>
                </div>
                <div className="data-item">
                  <label>Problema Relatado:</label>
                  <span>{serviceOrder.problemReported}</span>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'diagnostico' && (
            <div className="tab-pane">
              <h3>Diagnóstico e Orçamento</h3>
              <div className="form-group">
                <label>Descrição Técnica</label>
                <textarea
                  placeholder="Digite o diagnóstico técnico"
                  rows={4}
                  defaultValue={serviceOrder.diagnosis}
                />
              </div>
              <div className="form-group">
                <label>Peças Necessárias</label>
                <textarea
                  placeholder="Lista de peças"
                  rows={3}
                  defaultValue={serviceOrder.parts}
                />
              </div>
              <div className="form-group">
                <label>Horas de Serviço</label>
                <input type="number" placeholder="0.0" step="0.5" />
              </div>
              <div className="form-group">
                <label>Valor Total (R$)</label>
                <input type="number" placeholder="0.00" step="0.01" />
              </div>
              <button className="btn-primary">Enviar Orçamento</button>
            </div>
          )}

          {activeTab === 'checklist' && (
            <div className="tab-pane">
              <h3>Checklist do Veículo</h3>
              <div className="checklist-sections">
                <div className="checklist-section">
                  <h4>Motor</h4>
                  <div className="checklist-items">
                    {['Óleo', 'Fluido freio', 'Fluido direção', 'Água radiador', 'Correias', 'Bateria'].map(item => (
                      <label key={item} className="checkbox-label">
                        <input type="checkbox" />
                        <span>{item}</span>
                      </label>
                    ))}
                  </div>
                </div>
                <div className="checklist-section">
                  <h4>Suspensão</h4>
                  <div className="checklist-items">
                    {['Amortecedores', 'Molas', 'Buchas'].map(item => (
                      <label key={item} className="checkbox-label">
                        <input type="checkbox" />
                        <span>{item}</span>
                      </label>
                    ))}
                  </div>
                </div>
                <div className="checklist-section">
                  <h4>Freios</h4>
                  <div className="checklist-items">
                    {['Pastilhas', 'Discos', 'Lonas'].map(item => (
                      <label key={item} className="checkbox-label">
                        <input type="checkbox" />
                        <span>{item}</span>
                      </label>
                    ))}
                  </div>
                </div>
                <div className="checklist-section">
                  <h4>Pneus</h4>
                  <div className="checklist-items">
                    {['Dianteiro direito', 'Dianteiro esquerdo', 'Traseiro direito', 'Traseiro esquerdo', 'Estepe'].map(item => (
                      <label key={item} className="checkbox-label">
                        <input type="checkbox" />
                        <span>{item}</span>
                      </label>
                    ))}
                  </div>
                </div>
                <div className="checklist-section">
                  <h4>Segurança</h4>
                  <div className="checklist-items">
                    {['Triângulo', 'Macaco', 'Chave roda', 'Extintor', 'Manual', 'Chave reserva', 'CRLV'].map(item => (
                      <label key={item} className="checkbox-label">
                        <input type="checkbox" />
                        <span>{item}</span>
                      </label>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'fotos' && (
            <div className="tab-pane">
              <h3>Fotos e Vídeos</h3>
              <div className="photo-sections">
                <div className="photo-section">
                  <h4>Antes</h4>
                  <button className="btn-add-photo">+ Adicionar foto</button>
                  <button className="btn-add-video">+ Adicionar vídeo</button>
                </div>
                <div className="photo-section">
                  <h4>Durante</h4>
                  <button className="btn-add-photo">+ Adicionar foto</button>
                  <button className="btn-add-video">+ Adicionar vídeo</button>
                </div>
                <div className="photo-section">
                  <h4>Depois</h4>
                  <button className="btn-add-photo">+ Adicionar foto</button>
                  <button className="btn-add-video">+ Adicionar vídeo</button>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'historico' && (
            <div className="tab-pane">
              <h3>Histórico de Alterações</h3>
              <div className="timeline">
                <div className="timeline-item">
                  <div className="timeline-time">08:15</div>
                  <div className="timeline-content">
                    <strong>Recebido</strong>
                    <p>Ordem de serviço criada</p>
                  </div>
                </div>
                <div className="timeline-item">
                  <div className="timeline-time">08:22</div>
                  <div className="timeline-content">
                    <strong>Diagnóstico iniciado</strong>
                    <p>Iniciado atendimento</p>
                  </div>
                </div>
                <div className="timeline-item">
                  <div className="timeline-time">09:30</div>
                  <div className="timeline-content">
                    <strong>Orçamento enviado</strong>
                    <p>Aguardando aprovação do cliente</p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ServiceOrderDetails;