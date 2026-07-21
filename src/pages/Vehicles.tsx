import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api } from '../services/api';
import '../styles/Vehicles.css';

interface Vehicle {
  id: string;
  plate: string;
  brand: string;
  model: string;
  year: number;
  color: string;
  engine: string;
  fuel: string;
  mileage: number;
  chassis: string;
  renavam: string;
  customerId: string;
  customerName: string;
  serviceOrderCount: number;
  createdAt: string;
}

const Vehicles: React.FC = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const { hasPermission } = useAuth();

  useEffect(() => {
    loadVehicles();
  }, [search]);

  const loadVehicles = async () => {
    try {
      const url = search ? `/Vehicles?search=${encodeURIComponent(search)}` : '/Vehicles';
      const response = await api.get(url);
      const data = await response.json();
      setVehicles(data);
    } catch (error) {
      console.error('Erro ao carregar veículos:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="vehicles-page">
      <div className="page-header">
        <h1>Veículos</h1>
        {hasPermission('vehicles.write') && (
          <button className="btn btn-primary" onClick={() => alert('Funcionalidade em desenvolvimento')}>
            + Novo Veículo
          </button>
        )}
      </div>

      <div className="search-bar">
        <input
          type="text"
          placeholder="Buscar por placa, marca, modelo ou chassi..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="form-control"
        />
      </div>

      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Placa</th>
              <th>Marca</th>
              <th>Modelo</th>
              <th>Ano</th>
              <th>Cor</th>
              <th>Motor</th>
              <th>Combustível</th>
              <th>Quilometragem</th>
              <th>Proprietário</th>
              <th>Ordens</th>
            </tr>
          </thead>
          <tbody>
            {vehicles.map(vehicle => (
              <tr key={vehicle.id}>
                <td><strong>{vehicle.plate}</strong></td>
                <td>{vehicle.brand}</td>
                <td>{vehicle.model}</td>
                <td>{vehicle.year}</td>
                <td>{vehicle.color}</td>
                <td>{vehicle.engine}</td>
                <td>{vehicle.fuel}</td>
                <td>{vehicle.mileage.toLocaleString('pt-BR')} km</td>
                <td>{vehicle.customerName}</td>
                <td>{vehicle.serviceOrderCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {vehicles.length === 0 && (
        <div className="empty-state">
          <p>Nenhum veículo encontrado</p>
        </div>
      )}
    </div>
  );
};

export default Vehicles;