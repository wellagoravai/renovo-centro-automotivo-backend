import React from 'react';
import './About.css';

const About: React.FC = () => {
    return (
        <div className="about-page">
            <section className="about-header">
                <h1>Sobre a Renovo Centro Automotivo</h1>
                <p>Sua confiança em boas mãos</p>
            </section>

            <section className="about-content">
                <div className="container">
                    <div className="about-grid">
                        <div className="about-text">
                            <h2>Quem Somos</h2>
                            <p>
                                A Renovo Centro Automotivo é uma oficina especializada em manutenção e reparo de veículos, comprometida em oferecer segurança, confiança e qualidade em todos os serviços prestados.
                            </p>
                            <p>
                                Contamos com uma equipe de técnicos experientes e qualificados, além de equipamentos modernos para diagnosticar e resolver qualquer problema do seu veículo.
                            </p>
                        </div>

                        <div className="about-mission">
                            <h3>Nossa Missão</h3>
                            <p>Ser a oficina de confiança da comunidade de Andradina, oferecendo serviços de qualidade com comprometimento e transparência.</p>
                        </div>

                        <div className="about-values">
                            <h3>Nossos Valores</h3>
                            <ul>
                                <li>✅ Qualidade em tudo que fazemos</li>
                                <li>✅ Segurança e confiança</li>
                                <li>✅ Atendimento ao cliente excepcional</li>
                                <li>✅ Profissionalismo e ética</li>
                                <li>✅ Inovação e tecnologia</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </section>

            <section className="about-team">
                <div className="container">
                    <h2>Nossa Equipe</h2>
                    <p>Profissionais dedicados e qualificados a seu serviço</p>
                    <div className="team-grid">
                        <div className="team-member">
                            <div className="member-image">👨‍🔧</div>
                            <h4>Técnico Especialista</h4>
                            <p>Experiência em diagnóstico e reparo</p>
                        </div>
                        <div className="team-member">
                            <div className="member-image">👩‍🔧</div>
                            <h4>Mecânica Especializada</h4>
                            <p>Manutenção preventiva e corretiva</p>
                        </div>
                        <div className="team-member">
                            <div className="member-image">👨‍💼</div>
                            <h4>Gerente de Atendimento</h4>
                            <p>Suporte e consultoria ao cliente</p>
                        </div>
                    </div>
                </div>
            </section>

            <section className="about-commitment">
                <h2>Nosso Compromisso</h2>
                <p>
                    Falou em solucionar seu problema ou fazer sua manutenção preventiva, vem com a Renovo Centro Automotivo. Segurança, confiança e qualidade em cada serviço!
                </p>
            </section>
        </div>
    );
};

export default About;
