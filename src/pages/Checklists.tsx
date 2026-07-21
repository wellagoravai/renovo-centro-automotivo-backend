import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api } from '../services/api';
import '../styles/Checklists.css';

interface Checklist {
  id: string;
  vehiclePlate: string;
  vehicleBrand: string;
  vehicleModel: string;
  mileage: number;
  fuelLevel: string;
  generalState: string;
  observations: string;
  checkedAt: string;
  responsibleUser: string;
}

const Checklists: React.FC = () => {
  const [checklists, setChecklists] = useState<Checklist[]>([]);
  const [loading, setLoading] = useState(true);
  const { hasPermission } = useAuth();

  useEffect(() => {
    loadChecklists();
  }, []);

  const loadChecklists = async () => {
    try {
      const response = await api.get('/Checklists');
      const data = await response.json();
      setChecklists(data);
    } catch (error) {
      console.error('Erro ao carregar checklists:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="checklists-page">
      <div className="page-header">
        <h1>Checklists</h1>
        {hasPermission('orders.write') && (
          <button className="btn btn-primary" onClick={() => alert('Funcionalidade em desenvolvimento')}>
            + Novo Checklist
          </button>
        )}
      </div>

      <div className="checklists-grid">
        {checklists.map(checklist => (
          <div key={checklist.id} className="checklist-card">
            <div className="checklist-header">
              <h3>{checklist.vehiclePlate} - {checklist.vehicleBrand} {checklist.vehicleModel}</h3>
              <span className="checklist-date">
                {new Date(checklist.checkedAt).toLocaleDateString('pt-BR')}
              </span>
            </div>
            
            <div className="checklist-body">
              <div className="checklist-info">
                <div className="info-item">
                  <span className="info-label">Quilometragem:</span>
                  <span className="info-value">{checklist.mileage.toLocaleString('pt-BR')} km</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Combustível:</span>
                  <span className="info-value">{checklist.fuelLevel}</span>
                </div>
                <div className="info-item">
                  <span className="info-label">Responsável:</span>
                  <span className="info-value">{checklist.responsibleUser}</span>
                </div>
              </div>
              
              <div className="checklist-state">
                <strong>Estado Geral:</strong>
                <p>{checklist.generalState}</p>
              </div>
              
              {checklist.observations && (
                <div className="checklist-observations">
                  <strong>Observações:</strong>
                  <p>{checklist.observations}</p>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {checklists.length === 0 && (
        <div className="empty-state">
          <p>Nenhum checklist encontrado</p>
        </div>
      )}
    </div>
  );
};

export default Checklists;