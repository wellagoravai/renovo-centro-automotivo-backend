import React from 'react';

const Reports: React.FC = () => {
  return (
    <div className="reports-page">
      <h1>Relatórios</h1>
      <div className="reports-grid">
        <div className="report-card">
          <h3>Ordens de Serviço por Período</h3>
          <p>Relatório detalhado de ordens de serviço</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
        <div className="report-card">
          <h3>Faturamento</h3>
          <p>Análise de faturamento mensal</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
        <div className="report-card">
          <h3>Serviços Mais Realizados</h3>
          <p>Top 10 serviços mais solicitados</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
        <div className="report-card">
          <h3>Peças Mais Utilizadas</h3>
          <p>Controle de consumo de peças</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
        <div className="report-card">
          <h3>Desempenho de Funcionários</h3>
          <p>Produtividade da equipe</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
        <div className="report-card">
          <h3>Clientes Frequentes</h3>
          <p>Análise de frequência de clientes</p>
          <button className="btn-primary">Gerar Relatório</button>
        </div>
      </div>
    </div>
  );
};

export default Reports;