import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Services({ token }) {
    const [list, setList] = useState([]);
    const [form, setForm] = useState({ name: '', priceMotorcycle: '', priceCarSmall: '', priceCarLarge: '', durationMinutes: '', active: true });
    const [editing, setEditing] = useState(null);

    useEffect(() => {
        if (token) fetchServices();
    }, [token]);

    async function fetchServices() {
        try {
            const res = await axios.get('http://localhost:5000/services', { headers: { Authorization: 'Bearer ' + token } });
            setList(res.data);
        } catch (e) {
            alert('Erro ao buscar serviços');
        }
    }

    async function handleSubmit(e) {
        e.preventDefault();
        try {
            const data = {
                Name: form.name,
                PriceMotorcycle: parseFloat(form.priceMotorcycle),
                PriceCarSmall: parseFloat(form.priceCarSmall),
                PriceCarLarge: parseFloat(form.priceCarLarge),
                DurationMinutes: parseInt(form.durationMinutes),
                Active: form.active
            };
            if (editing) {
                await axios.put(`http://localhost:5000/services/${editing}`, data, { headers: { Authorization: 'Bearer ' + token } });
                setEditing(null);
            } else {
                await axios.post('http://localhost:5000/services', data, { headers: { Authorization: 'Bearer ' + token } });
            }
            setForm({ name: '', priceMotorcycle: '', priceCarSmall: '', priceCarLarge: '', durationMinutes: '', active: true });
            fetchServices();
        } catch (e) {
            alert('Erro ao salvar serviço');
        }
    }

    async function handleDelete(id) {
        if (!confirm('Deseja realmente excluir este serviço?')) return;
        try {
            await axios.delete(`http://localhost:5000/services/${id}`, { headers: { Authorization: 'Bearer ' + token } });
            fetchServices();
        } catch (e) {
            alert('Erro ao excluir serviço');
        }
    }

    function handleEdit(service) {
        setEditing(service.id);
        setForm({
            name: service.name,
            priceMotorcycle: service.priceMotorcycle,
            priceCarSmall: service.priceCarSmall,
            priceCarLarge: service.priceCarLarge,
            durationMinutes: service.durationMinutes,
            active: service.active
        });
    }

    function handleCancel() {
        setEditing(null);
        setForm({ name: '', priceMotorcycle: '', priceCarSmall: '', priceCarLarge: '', durationMinutes: '', active: true });
    }

    return (
        <div>
            <h2>Serviços</h2>
            <form onSubmit={handleSubmit} className="form-card">
                <input placeholder="Nome do Serviço" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
                <input placeholder="Preço Motocicleta" type="number" step="0.01" value={form.priceMotorcycle} onChange={e => setForm({ ...form, priceMotorcycle: e.target.value })} required />
                <input placeholder="Preço Carro Pequeno" type="number" step="0.01" value={form.priceCarSmall} onChange={e => setForm({ ...form, priceCarSmall: e.target.value })} required />
                <input placeholder="Preço Carro Grande" type="number" step="0.01" value={form.priceCarLarge} onChange={e => setForm({ ...form, priceCarLarge: e.target.value })} required />
                <input placeholder="Duração (minutos)" type="number" value={form.durationMinutes} onChange={e => setForm({ ...form, durationMinutes: e.target.value })} required />
                <label>
                    <input type="checkbox" checked={form.active} onChange={e => setForm({ ...form, active: e.target.checked })} />
                    Ativo
                </label>
                <div className="form-actions">
                    <button type="submit">{editing ? 'Atualizar' : 'Adicionar'}</button>
                    {editing && <button type="button" onClick={handleCancel}>Cancelar</button>}
                </div>
            </form>
            <table>
                <thead>
                    <tr>
                        <th>Nome</th>
                        <th>Moto</th>
                        <th>Carro Peq.</th>
                        <th>Carro Grande</th>
                        <th>Duração</th>
                        <th>Status</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(s => (
                        <tr key={s.id}>
                            <td>{s.name}</td>
                            <td>R$ {s.priceMotorcycle.toFixed(2)}</td>
                            <td>R$ {s.priceCarSmall.toFixed(2)}</td>
                            <td>R$ {s.priceCarLarge.toFixed(2)}</td>
                            <td>{s.durationMinutes} min</td>
                            <td>{s.active ? 'Ativo' : 'Inativo'}</td>
                            <td>
                                <button onClick={() => handleEdit(s)}>Editar</button>
                                <button onClick={() => handleDelete(s.id)}>Excluir</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

