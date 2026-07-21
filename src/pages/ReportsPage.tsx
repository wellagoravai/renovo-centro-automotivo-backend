import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import '../styles/Reports.css';

interface ServiceOrder {
  id: string;
  number: string;
  status: string;
  entryDate: string;
  customerName: string;
  vehiclePlate: string;
  value: number;
}

interface ReportData {
  totalOrders: number;
  totalValue: number;
  orders: ServiceOrder[];
}

interface CompletedMaintenance {
  id: string;
  orderNumber: string;
  customerName: string;
  vehiclePlate: string;
  vehicleBrand: string;
  vehicleModel: string;
  services: string;
  parts: string;
  value: number;
  entryDate: string;
  finalDate: string;
  responsibleUser: string;
  durationHours: number;
}

interface ServiceStat {
  serviceName: string;
  count: number;
  totalValue: number;
}

interface TopService {
  serviceName: string;
  count: number;
  totalRevenue: number;
}

type ReportTab = 'overview' | 'completed' | 'top-services' | 'revenue';

const ReportsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ReportTab>('overview');
  const [loading, setLoading] = useState(false);
  
  // Overview
  const [overviewData, setOverviewData] = useState<any>(null);
  
  // Completed Maintenance
  const [completedData, setCompletedData] = useState<{
    summary: any;
    data: CompletedMaintenance[];
  } | null>(null);
  const [completedStartDate, setCompletedStartDate] = useState(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  );
  const [completedEndDate, setCompletedEndDate] = useState(
    new Date().toISOString().split('T')[0]
  );

  // Top Services
  const [topServicesData, setTopServicesData] = useState<{
    period: string;
    startDate: Date;
    endDate: Date;
    totalOrders: number;
    services: TopService[];
  } | null>(null);
  const [topServicesPeriod, setTopServicesPeriod] = useState<'day' | 'week' | 'month'>('week');

  // Revenue
  const [revenueData, setRevenueData] = useState<any>(null);
  const [revenueStartDate, setRevenueStartDate] = useState(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  );
  const [revenueEndDate, setRevenueEndDate] = useState(
    new Date().toISOString().split('T')[0]
  );

  // Service Orders by Period (original)
  const [reportType, setReportType] = useState<'day' | 'week' | 'month'>('day');
  const [reportData, setReportData] = useState<ReportData | null>(null);
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split('T')[0]);

  useEffect(() => {
    loadOverview();
  }, []);

  const loadOverview = async () => {
    setLoading(true);
    try {
      const response = await api.get('/reports/services-overview');
      if (response.ok) {
        const data = await response.json();
        setOverviewData(data);
      }
    } catch (error) {
      console.error('Erro ao carregar visão geral:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadCompletedMaintenance = async () => {
    setLoading(true);
    try {
      const response = await api.get(
        `/reports/completed-maintenance?startDate=${completedStartDate}&endDate=${completedEndDate}`
      );
      if (response.ok) {
        const data = await response.json();
        setCompletedData(data);
      }
    } catch (error) {
      console.error('Erro ao carregar manutenções concluídas:', error);
      alert('❌ Erro ao carregar relatório');
    } finally {
      setLoading(false);
    }
  };

  const loadTopServices = async () => {
    setLoading(true);
    try {
      const response = await api.get(`/reports/top-services?period=${topServicesPeriod}&top=10`);
      if (response.ok) {
        const data = await response.json();
        setTopServicesData(data);
      }
    } catch (error) {
      console.error('Erro ao carregar serviços mais efetuados:', error);
      alert('❌ Erro ao carregar relatório');
    } finally {
      setLoading(false);
    }
  };

  const loadRevenue = async () => {
    setLoading(true);
    try {
      const response = await api.get(
        `/reports/revenue?startDate=${revenueStartDate}&endDate=${revenueEndDate}`
      );
      if (response.ok) {
        const data = await response.json();
        setRevenueData(data);
      }
    } catch (error) {
      console.error('Erro ao carregar faturamento:', error);
      alert('❌ Erro ao carregar relatório');
    } finally {
      setLoading(false);
    }
  };

  const generateReport = async () => {
    setLoading(true);
    try {
      const startDate = new Date(selectedDate);
      const endDate = new Date(selectedDate);

      if (reportType === 'week') {
        const day = startDate.getDay();
        const diff = startDate.getDate() - day + (day === 0 ? -6 : 1);
        startDate.setDate(diff);
        endDate.setDate(startDate.getDate() + 6);
      } else if (reportType === 'month') {
        startDate.setDate(1);
        endDate.setMonth(endDate.getMonth() + 1);
        endDate.setDate(0);
      }

      const url = `/Reports/service-orders?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
      const response = await api.get(url);

      if (response.ok) {
        const data = await response.json();
        setReportData(data);
      } else {
        alert('❌ Erro ao gerar relatório');
      }
    } catch (error) {
      console.error('Erro ao gerar relatório:', error);
      alert('❌ Erro ao gerar relatório');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('pt-BR');
  };

  const getStatusColor = (status: string) => {
    const colors: any = {
      'Recebido': '#3498db',
      'Em Diagnóstico': '#f39c12',
      'Orçamento': '#1abc9c',
      'Aguardando Aprovação': '#e74c3c',
      'Aprovado': '#27ae60',
      'Em Manutenção': '#3498db',
      'Entregue': '#95a5a6',
      'Cancelado': '#e74c3c',
      'Concluído': '#27ae60',
    };
    return colors[status] || '#7f8c8d';
  };

  const getReportTitle = () => {
    switch (reportType) {
      case 'day':
        return 'Relatório Diário';
      case 'week':
        return 'Relatório Semanal';
      case 'month':
        return 'Relatório Mensal';
    }
  };

  return (
    <div className="reports-page">
      <div className="page-header">
        <h1>📊 Relatórios</h1>
      </div>

      {/* Tabs */}
      <div className="tabs-container">
        <div className="tabs">
          <button
            className={`tab ${activeTab === 'overview' ? 'active' : ''}`}
            onClick={() => setActiveTab('overview')}
          >
            📈 Visão Geral
          </button>
          <button
            className={`tab ${activeTab === 'completed' ? 'active' : ''}`}
            onClick={() => {
              setActiveTab('completed');
              loadCompletedMaintenance();
            }}
          >
            ✅ Manutenções Concluídas
          </button>
          <button
            className={`tab ${activeTab === 'top-services' ? 'active' : ''}`}
            onClick={() => {
              setActiveTab('top-services');
              loadTopServices();
            }}
          >
            🔧 Serviços Mais Realizados
          </button>
          <button
            className={`tab ${activeTab === 'revenue' ? 'active' : ''}`}
            onClick={() => {
              setActiveTab('revenue');
              loadRevenue();
            }}
          >
            💰 Faturamento
          </button>
        </div>
      </div>

      {loading && <div className="loading">Carregando...</div>}

      {/* Visão Geral */}
      {activeTab === 'overview' && !loading && (
        <div className="report-section">
          <h2>Visão Geral de Serviços</h2>
          
          {overviewData && (
            <>
              <div className="report-summary">
                <div className="summary-card">
                  <h3>Total de Ordens</h3>
                  <p className="summary-value">{overviewData.totalOrders}</p>
                </div>
                <div className="summary-card">
                  <h3>Valor Total</h3>
                  <p className="summary-value">{formatCurrency(overviewData.totalValue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Período</h3>
                  <p className="summary-value">
                    {formatDate(overviewData.Period.start.toString())} - {formatDate(overviewData.Period.end.toString())}
                  </p>
                </div>
              </div>

              <div className="report-grid">
                <div className="report-card">
                  <h3>Serviços Realizados</h3>
                  <div className="services-list">
                    {overviewData.Services.slice(0, 10).map((service: ServiceStat, index: number) => (
                      <div key={index} className="service-item">
                        <div className="service-info">
                          <span className="service-name">{service.serviceName}</span>
                          <span className="service-count">{service.count}x</span>
                        </div>
                        <div className="service-value">{formatCurrency(service.totalValue)}</div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="report-card">
                  <h3>Status das Ordens</h3>
                  <div className="status-breakdown">
                    {overviewData.StatusBreakdown.map((item: any, index: number) => (
                      <div key={index} className="status-item">
                        <span 
                          className="status-badge" 
                          style={{ backgroundColor: getStatusColor(item.Status) }}
                        >
                          {item.Status}
                        </span>
                        <span className="status-count">{item.Count}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {/* Manutenções Concluídas */}
      {activeTab === 'completed' && !loading && (
        <div className="report-section">
          <h2>Manutenções Concluídas</h2>
          
          <div className="report-filters">
            <div className="filter-group">
              <label>Data Inicial:</label>
              <input
                type="date"
                value={completedStartDate}
                onChange={(e) => setCompletedStartDate(e.target.value)}
                className="form-control"
              />
            </div>
            <div className="filter-group">
              <label>Data Final:</label>
              <input
                type="date"
                value={completedEndDate}
                onChange={(e) => setCompletedEndDate(e.target.value)}
                className="form-control"
              />
            </div>
            <button
              className="btn btn-primary"
              onClick={loadCompletedMaintenance}
            >
              🔍 Filtrar
            </button>
          </div>

          {completedData && (
            <>
              <div className="report-summary">
                <div className="summary-card">
                  <h3>Total Concluídas</h3>
                  <p className="summary-value">{completedData.summary.TotalCompleted}</p>
                </div>
                <div className="summary-card">
                  <h3>Valor Total</h3>
                  <p className="summary-value">{formatCurrency(completedData.summary.TotalValue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Valor Médio</h3>
                  <p className="summary-value">{formatCurrency(completedData.summary.AverageValue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Duração Média</h3>
                  <p className="summary-value">{completedData.summary.AverageDuration.toFixed(1)}h</p>
                </div>
              </div>

              <div className="report-table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>OS</th>
                      <th>Data Entrada</th>
                      <th>Data Conclusão</th>
                      <th>Cliente</th>
                      <th>Veículo</th>
                      <th>Serviços</th>
                      <th>Responsável</th>
                      <th>Duração</th>
                      <th>Valor</th>
                    </tr>
                  </thead>
                  <tbody>
                    {completedData.data.map(order => (
                      <tr key={order.id}>
                        <td><strong>{order.orderNumber}</strong></td>
                        <td>{formatDate(order.entryDate)}</td>
                        <td>{formatDate(order.finalDate)}</td>
                        <td>{order.customerName}</td>
                        <td>{order.vehiclePlate} - {order.vehicleBrand} {order.vehicleModel}</td>
                        <td>{order.services}</td>
                        <td>{order.responsibleUser}</td>
                        <td>{order.durationHours.toFixed(1)}h</td>
                        <td><strong>{formatCurrency(order.value)}</strong></td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {completedData.data.length === 0 && (
                  <div className="empty-state">
                    <p>Nenhuma manutenção concluída no período</p>
                  </div>
                )}
              </div>

              <div className="report-actions">
                <button
                  className="btn btn-primary"
                  onClick={() => window.print()}
                >
                  🖨️ Imprimir Relatório
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {/* Serviços Mais Realizados */}
      {activeTab === 'top-services' && !loading && (
        <div className="report-section">
          <h2>Serviços Mais Realizados</h2>

          <div className="report-filters">
            <div className="filter-group">
              <label>Período:</label>
              <div className="button-group">
                <button
                  className={`btn ${topServicesPeriod === 'day' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => {
                    setTopServicesPeriod('day');
                    loadTopServices();
                  }}
                >
                  📅 Hoje
                </button>
                <button
                  className={`btn ${topServicesPeriod === 'week' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => {
                    setTopServicesPeriod('week');
                    loadTopServices();
                  }}
                >
                  📆 Semana
                </button>
                <button
                  className={`btn ${topServicesPeriod === 'month' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => {
                    setTopServicesPeriod('month');
                    loadTopServices();
                  }}
                >
                  📊 Mês
                </button>
              </div>
            </div>
          </div>

          {topServicesData && (
            <>
              <div className="report-summary">
                <div className="summary-card">
                  <h3>Período</h3>
                  <p className="summary-value">
                    {topServicesPeriod === 'day' ? 'Hoje' : 
                     topServicesPeriod === 'week' ? 'Últimos 7 dias' : 'Últimos 30 dias'}
                  </p>
                </div>
                <div className="summary-card">
                  <h3>Total de Ordens</h3>
                  <p className="summary-value">{topServicesData.totalOrders}</p>
                </div>
                <div className="summary-card">
                  <h3>Serviços Diferentes</h3>
                  <p className="summary-value">{topServicesData.services.length}</p>
                </div>
              </div>

              <div className="report-table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Serviço</th>
                      <th>Quantidade</th>
                      <th>Faturamento</th>
                      <th>% do Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {topServicesData.services.map((service, index) => {
                      const percentage = topServicesData.totalOrders > 0 
                        ? ((service.count / topServicesData.totalOrders) * 100).toFixed(1) 
                        : 0;
                      return (
                        <tr key={index}>
                          <td><strong>#{index + 1}</strong></td>
                          <td>{service.serviceName}</td>
                          <td><span className="badge badge-primary">{service.count}x</span></td>
                          <td><strong>{formatCurrency(service.totalRevenue)}</strong></td>
                          <td>
                            <div className="progress-bar">
                              <div 
                                className="progress-fill" 
                                style={{ width: `${percentage}%` }}
                              ></div>
                              <span className="progress-text">{percentage}%</span>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>

                {topServicesData.services.length === 0 && (
                  <div className="empty-state">
                    <p>Nenhum serviço encontrado no período</p>
                  </div>
                )}
              </div>

              <div className="report-actions">
                <button
                  className="btn btn-primary"
                  onClick={() => window.print()}
                >
                  🖨️ Imprimir Relatório
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {/* Faturamento */}
      {activeTab === 'revenue' && !loading && (
        <div className="report-section">
          <h2>Relatório de Faturamento</h2>

          <div className="report-filters">
            <div className="filter-group">
              <label>Data Inicial:</label>
              <input
                type="date"
                value={revenueStartDate}
                onChange={(e) => setRevenueStartDate(e.target.value)}
                className="form-control"
              />
            </div>
            <div className="filter-group">
              <label>Data Final:</label>
              <input
                type="date"
                value={revenueEndDate}
                onChange={(e) => setRevenueEndDate(e.target.value)}
                className="form-control"
              />
            </div>
            <button
              className="btn btn-primary"
              onClick={loadRevenue}
            >
              🔍 Filtrar
            </button>
          </div>

          {revenueData && (
            <>
              <div className="report-summary">
                <div className="summary-card">
                  <h3>Faturamento Total</h3>
                  <p className="summary-value">{formatCurrency(revenueData.totalRevenue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Ticket Médio</h3>
                  <p className="summary-value">{formatCurrency(revenueData.averageOrderValue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Total de Ordens</h3>
                  <p className="summary-value">{revenueData.totalOrders}</p>
                </div>
              </div>

              <div className="report-card">
                <h3>Faturamento por Dia</h3>
                <div className="revenue-chart">
                  {revenueData.dailyRevenue.map((day: any, index: number) => (
                    <div key={index} className="revenue-day">
                      <div className="revenue-bar-container">
                        <div 
                          className="revenue-bar" 
                          style={{ 
                            height: `${(day.Revenue / Math.max(...revenueData.dailyRevenue.map((d: any) => d.Revenue))) * 100}%` 
                          }}
                        ></div>
                      </div>
                      <div className="revenue-info">
                        <span className="revenue-date">{formatDate(day.Date.toString())}</span>
                        <span className="revenue-amount">{formatCurrency(day.Revenue)}</span>
                        <span className="revenue-count">{day.Count} OS</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="report-actions">
                <button
                  className="btn btn-primary"
                  onClick={() => window.print()}
                >
                  🖨️ Imprimir Relatório
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {/* Relatório de Ordens por Período (original) */}
      {activeTab === 'overview' && !loading && (
        <div className="report-section">
          <h2>Ordens de Serviço por Período</h2>
          
          <div className="report-filters">
            <div className="filter-group">
              <label>Tipo de Relatório:</label>
              <div className="button-group">
                <button
                  className={`btn ${reportType === 'day' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setReportType('day')}
                >
                  📅 Diário
                </button>
                <button
                  className={`btn ${reportType === 'week' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setReportType('week')}
                >
                  📆 Semanal
                </button>
                <button
                  className={`btn ${reportType === 'month' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setReportType('month')}
                >
                  📊 Mensal
                </button>
              </div>
            </div>

            <div className="filter-group">
              <label>Data {reportType === 'day' ? '' : 'Inicial'}:</label>
              <input
                type="date"
                value={selectedDate}
                onChange={(e) => setSelectedDate(e.target.value)}
                className="form-control"
              />
            </div>

            <button
              className="btn btn-success"
              onClick={generateReport}
              disabled={loading}
            >
              {loading ? '⏳ Gerando...' : '📈 Gerar Relatório'}
            </button>
          </div>

          {reportData && (
            <div className="report-results">
              <div className="report-summary">
                <div className="summary-card">
                  <h3>Total de Ordens</h3>
                  <p className="summary-value">{reportData.totalOrders}</p>
                </div>
                <div className="summary-card">
                  <h3>Valor Total</h3>
                  <p className="summary-value">{formatCurrency(reportData.totalValue)}</p>
                </div>
                <div className="summary-card">
                  <h3>Período</h3>
                  <p className="summary-value">{getReportTitle()}</p>
                </div>
              </div>

              <div className="report-table-container">
                <h3>Ordens de Serviço - {getReportTitle()}</h3>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Número</th>
                      <th>Data Entrada</th>
                      <th>Cliente</th>
                      <th>Veículo</th>
                      <th>Status</th>
                      <th>Valor</th>
                    </tr>
                  </thead>
                  <tbody>
                    {reportData.orders.map(order => (
                      <tr key={order.id}>
                        <td><strong>OS {order.number}</strong></td>
                        <td>{new Date(order.entryDate).toLocaleDateString('pt-BR')}</td>
                        <td>{order.customerName}</td>
                        <td>{order.vehiclePlate}</td>
                        <td>
                          <span
                            className="badge"
                            style={{ backgroundColor: getStatusColor(order.status) }}
                          >
                            {order.status}
                          </span>
                        </td>
                        <td>{formatCurrency(order.value)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {reportData.orders.length === 0 && (
                  <div className="empty-state">
                    <p>Nenhuma ordem de serviço encontrada no período</p>
                  </div>
                )}
              </div>

              <div className="report-actions">
                <button
                  className="btn btn-primary"
                  onClick={() => window.print()}
                >
                  🖨️ Imprimir Relatório
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={() => {
                    const csv = reportData.orders.map(o =>
                      `${o.number},${new Date(o.entryDate).toLocaleDateString('pt-BR')},${o.customerName},${o.vehiclePlate},${o.status},${o.value}`
                    ).join('\n');
                    const header = 'Número,Data Entrada,Cliente,Veículo,Status,Valor\n';
                    const blob = new Blob([header + csv], { type: 'text/csv' });
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = `relatorio-${reportType}-${selectedDate}.csv`;
                    a.click();
                  }}
                >
                  📥 Exportar CSV
                </button>
              </div>
            </div>
          )}

          {!reportData && !loading && (
            <div className="empty-state">
              <p>Selecione o tipo de relatório e clique em "Gerar Relatório"</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default ReportsPage;