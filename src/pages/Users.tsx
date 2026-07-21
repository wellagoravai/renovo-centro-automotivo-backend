import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { api } from '../services/api';
import '../styles/Users.css';

interface User {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

const Users: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const { hasPermission } = useAuth();

  useEffect(() => {
    loadUsers();
  }, [search]);

  const loadUsers = async () => {
    try {
      const url = search ? `/Users?search=${encodeURIComponent(search)}` : '/Users';
      const response = await api.get(url);
      const data = await response.json();
      setUsers(data);
    } catch (error) {
      console.error('Erro ao carregar usuários:', error);
    } finally {
      setLoading(false);
    }
  };

  const getRoleLabel = (role: string) => {
    const labels: any = {
      'Administrador': 'Administrador',
      'Gerente': 'Gerente',
      'Recepção': 'Recepção',
      'Mecânico': 'Mecânico',
      'Almoxarifado': 'Almoxarifado',
      'Cliente': 'Cliente'
    };
    return labels[role] || role;
  };

  if (loading) {
    return <div className="loading">Carregando...</div>;
  }

  return (
    <div className="users-page">
      <div className="page-header">
        <h1>Usuários</h1>
        {hasPermission('users.manage') && (
          <button className="btn btn-primary" onClick={() => alert('Funcionalidade em desenvolvimento')}>
            + Novo Usuário
          </button>
        )}
      </div>

      <div className="search-bar">
        <input
          type="text"
          placeholder="Buscar por nome, usuário ou email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="form-control"
        />
      </div>

      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>Usuário</th>
              <th>Email</th>
              <th>Perfil</th>
              <th>Status</th>
              <th>Data Criação</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.id}>
                <td><strong>{user.fullName}</strong></td>
                <td>{user.userName}</td>
                <td>{user.email}</td>
                <td>
                  <span className="badge badge-info">{getRoleLabel(user.role)}</span>
                </td>
                <td>
                  <span className={`badge ${user.isActive ? 'badge-success' : 'badge-danger'}`}>
                    {user.isActive ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td>{new Date(user.createdAt).toLocaleDateString('pt-BR')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {users.length === 0 && (
        <div className="empty-state">
          <p>Nenhum usuário encontrado</p>
        </div>
      )}
    </div>
  );
};

export default Users;