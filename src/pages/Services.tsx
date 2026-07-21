import React from 'react';
import './Services.css';

const Services: React.FC = () => {
    const services = [
        {
            id: '1',
            name: 'Manutenção Preventiva',
            icon: '🛠️',
            description: 'Mantenha seu veículo sempre em perfeito estado com nossa manutenção preventiva especializada.',
            details: 'Inclui: Troca de óleo, verificação de filtros, inspeção de pastilhas e revisão geral.',
        },
        {
            id: '2',
            name: 'Reparos Gerais',
            icon: '🔧',
            description: 'Reparamos qualquer tipo de problema no seu veículo com técnicos experientes.',
            details: 'Reparação de motor, câmbio, suspensão e outros sistemas.',
        },
        {
            id: '3',
            name: 'Diagnóstico Completo',
            icon: '🔍',
            description: 'Utilizamos equipamentos de diagnóstico de última geração.',
            details: 'Identificamos problemas precisamente com scanner automotivo de alta precisão.',
        },
        {
            id: '4',
            name: 'Alinhamento e Balanceamento',
            icon: '⚙️',
            description: 'Garantimos alinhamento e balanceamento com máquinas de precisão.',
            details: 'Melhora na segurança, conforto e desempenho do seu veículo.',
        },
        {
            id: '5',
            name: 'Freios e Pastilhas',
            icon: '🛑',
            description: 'Sistema de freios em perfeito estado para sua segurança.',
            details: 'Revisão, troca de pastilhas e sangria de freios.',
        },
        {
            id: '6',
            name: 'Suspensão',
            icon: '🚗',
            description: 'Garantimos conforto e estabilidade do seu veículo.',
            details: 'Inspeção e reparação de amortecedores e molas.',
        },
    ];

    return (
        <div className="services-page">
            <section className="services-header">
                <h1>Nossos Serviços</h1>
                <p>Conheça a gama completa de serviços da Renovo Centro Automotivo</p>
            </section>

            <section className="services-content">
                <div className="container">
                    <div className="services-grid">
                        {services.map((service) => (
                            <div key={service.id} className="service-box">
                                <div className="service-icon">{service.icon}</div>
                                <h3>{service.name}</h3>
                                <p className="description">{service.description}</p>
                                <p className="details">{service.details}</p>
                                <button className="button">Solicitar Serviço</button>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            <section className="services-cta">
                <h2>Agende seu Serviço Agora</h2>
                <p>Ligue para nós ou use nosso chat para agendar um horário que seja conveniente para você.</p>
                <div className="cta-buttons">
                    <button className="button">📞 Ligar Agora</button>
                    <button className="button secondary-button">💬 Usar Chat</button>
                </div>
            </section>
        </div>
    );
};

export default Services;
