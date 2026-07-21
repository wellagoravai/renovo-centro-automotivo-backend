import React from 'react';

const Settings: React.FC = () => {
  return (
    <div className="settings-page">
      <h1>Configurações</h1>
      <div className="settings-sections">
        <div className="settings-section">
          <h2>Geral</h2>
          <div className="setting-item">
            <label>Nome da Oficina</label>
            <input type="text" defaultValue="Renovo Workshop" />
          </div>
          <div className="setting-item">
            <label>Telefone</label>
            <input type="tel" defaultValue="(11) 98765-4321" />
          </div>
          <div className="setting-item">
            <label>Email</label>
            <input type="email" defaultValue="contato@renovo.com.br" />
          </div>
        </div>

        <div className="settings-section">
          <h2>Notificações</h2>
          <div className="setting-item">
            <label>
              <input type="checkbox" defaultChecked />
              Notificar quando nova OS for criada
            </label>
          </div>
          <div className="setting-item">
            <label>
              <input type="checkbox" defaultChecked />
              Notificar quando orçamento for aprovado
            </label>
          </div>
          <div className="setting-item">
            <label>
              <input type="checkbox" defaultChecked />
              Notificar quando veículo for entregue
            </label>
          </div>
        </div>

        <div className="settings-section">
          <h2>Backup</h2>
          <div className="setting-item">
            <label>Backup automático</label>
            <select defaultValue="daily">
              <option value="daily">Diário</option>
              <option value="weekly">Semanal</option>
              <option value="monthly">Mensal</option>
            </select>
          </div>
          <button className="btn-primary">Fazer Backup Agora</button>
        </div>
      </div>
    </div>
  );
};

export default Settings;