import React, { useState, useEffect } from 'react';
import { api } from '../services/api';

interface WorkshopSettings {
  id: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  logo?: string;
}

const SettingsPage: React.FC = () => {
  const [settings, setSettings] = useState<WorkshopSettings>({
    id: '',
    name: 'Renovo Workshop',
    phone: '',
    email: '',
    address: '',
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      const response = await api.get('/Settings');
      if (response.ok) {
        const data = await response.json();
        setSettings(data);
      }
    } catch (error) {
      console.error('Erro ao carregar configurações:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);

    try {
      const response = await api.put('/Settings', settings);

      if (response.ok) {
        alert('✅ Configurações salvas com sucesso!');
      } else {
        const error = await response.json();
        alert(`❌ Erro: ${error.message || 'Erro ao salvar configurações'}`);
      }
    } catch (error) {
      console.error('Erro ao salvar configurações:', error);
      alert('❌ Erro ao salvar configurações');
    } finally {
      setSaving(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setSettings({
      ...settings,
      [e.target.name]: e.target.value,
    });
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="settings-page">
      <div className="page-header">
        <h1>⚙️ Configurações</h1>
      </div>

      <div className="settings-container">
        <form onSubmit={handleSubmit}>
          <div className="settings-section">
            <h2>🏢 Informações da Oficina</h2>
            <div className="form-group">
              <label>Nome da Oficina *</label>
              <input
                type="text"
                name="name"
                value={settings.name}
                onChange={handleChange}
                placeholder="Ex: Renovo Workshop"
                required
              />
            </div>
            <div className="form-group">
              <label>Telefone *</label>
              <input
                type="tel"
                name="phone"
                value={settings.phone}
                onChange={handleChange}
                placeholder="(00) 00000-0000"
                required
              />
            </div>
            <div className="form-group">
              <label>Email *</label>
              <input
                type="email"
                name="email"
                value={settings.email}
                onChange={handleChange}
                placeholder="contato@oficina.com.br"
                required
              />
            </div>
            <div className="form-group">
              <label>Endereço</label>
              <textarea
                name="address"
                value={settings.address}
                onChange={handleChange}
                placeholder="Rua, número, bairro, cidade - UF"
                rows={3}
              />
            </div>
          </div>

          <div className="settings-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? '⏳ Salvando...' : '💾 Salvar Informações'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default SettingsPage;