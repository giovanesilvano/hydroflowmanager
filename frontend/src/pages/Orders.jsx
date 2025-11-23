import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Orders({ token }) {
    const [orders, setOrders] = useState([]);
    const [services, setServices] = useState([]);
    const [vehicles, setVehicles] = useState([]);
    const [editingOrder, setEditingOrder] = useState(null);
    const [form, setForm] = useState({
        vehiclePlate: '',
        paymentMethod: 0,
        items: []
    });

    useEffect(() => {
        fetchData();
    }, []);

    async function fetchData() {
        console.log('📥 Carregando dados...');
        try {
            const [ordersRes, servicesRes, vehiclesRes] = await Promise.all([
                axios.get('http://localhost:5000/orders', {
                    headers: { Authorization: 'Bearer ' + token }
                }),
                axios.get('http://localhost:5000/services', {
                    headers: { Authorization: 'Bearer ' + token }
                }),
                axios.get('http://localhost:5000/vehicles', {
                    headers: { Authorization: 'Bearer ' + token }
                })
            ]);

            setOrders(ordersRes.data);
            setServices(servicesRes.data);
            setVehicles(vehiclesRes.data);
            console.log('✅ Dados carregados:', {
                ordens: ordersRes.data.length,
                servicos: servicesRes.data.length,
                veiculos: vehiclesRes.data.length
            });
        } catch (e) {
            console.error('❌ Erro ao carregar dados:', e.response?.data || e);
            alert('Erro ao carregar dados de ordens/serviços/veículos');
        }
    }

    function resetForm() {
        console.log('🔄 Resetando formulário');
        setEditingOrder(null);
        setForm({
            vehiclePlate: '',
            paymentMethod: 0,
            items: []
        });
    }

    function addItem() {
        console.log('➕ Adicionando item ao formulário');
        setForm(f => ({
            ...f,
            items: [...f.items, { serviceId: services[0]?.id || 0, quantity: 1 }]
        }));
    }

    function updateItem(index, field, value) {
        console.log(`✏️ Atualizando item ${index}: ${field} = ${value}`);
        setForm(f => {
            const items = [...f.items];
            items[index] = { ...items[index], [field]: parseInt(value) };
            return { ...f, items };
        });
    }

    function removeItem(index) {
        console.log(`🗑️ Removendo item ${index}`);
        setForm(f => ({
            ...f,
            items: f.items.filter((_, i) => i !== index)
        }));
    }

    async function handleCreate(e) {
        e.preventDefault();
        console.log('🆕 handleCreate chamado');
        try {
            const payload = {
                vehiclePlate: form.vehiclePlate,
                paymentMethod: parseInt(form.paymentMethod),
                attendantCPF: '00000000000', // ⚠️ Ajuste conforme necessário
                items: form.items.map(it => ({
                    serviceId: parseInt(it.serviceId),
                    quantity: parseInt(it.quantity)
                }))
            };
            console.log('📤 Enviando POST /orders com payload:', payload);

            const response = await axios.post('http://localhost:5000/orders', payload, {
                headers: { Authorization: 'Bearer ' + token }
            });

            console.log('✅ Ordem criada com sucesso:', response.data);
            alert('Ordem criada com sucesso!');
            resetForm();
            await fetchData();
        } catch (e) {
            console.error('❌ Erro ao criar ordem:', e.response?.data || e);
            alert('Erro ao criar ordem: ' + (e.response?.data || e.message));
        }
    }

    async function handleUpdate(e) {
        e.preventDefault();
        console.log('🔧 handleUpdate TESTE chamado, editingOrder =', editingOrder, 'form =', form);

        try {
            const payload = {
                paymentMethod: 0, // TESTE: sempre 0 (Dinheiro)
                items: form.items.map(it => ({
                    serviceId: Number(it.serviceId),
                    quantity: Number(it.quantity)
                }))
            };

            console.log(
                '   (TESTE) Enviando PUT /orders/{id} com payload:',
                JSON.stringify(payload)
            );

            const resp = await axios.put(
                `http://localhost:5000/orders/${editingOrder.id}`,
                payload,
                { headers: { Authorization: 'Bearer ' + token } }
            );

            console.log(
                '   Resposta do backend em /orders/{id}:',
                resp.status,
                resp.data
            );
            alert('Ordem atualizada com sucesso! (teste)');
            resetForm();
            await fetchData();
        } catch (e) {
            console.error('❌ Erro ao atualizar ordem (teste):', e.response || e);
            alert('Erro ao atualizar ordem (teste): ' + (e.response?.data || e.message));
        }
    }

    async function handlePay(order) {
        console.log('💰 handlePay chamado');
        console.log('   Ordem:', order);

        if (!order || !order.id) {
            console.error('❌ Ordem inválida!');
            alert('Erro: ordem inválida');
            return;
        }

        try {
            const payload = {
                paymentMethod: order.paymentMethod ?? 0
            };
            console.log(`📤 Enviando PUT /orders/${order.id}/pay com payload:`, payload);

            const response = await axios.put(
                `http://localhost:5000/orders/${order.id}/pay`,
                payload,
                {
                    headers: { Authorization: 'Bearer ' + token }
                }
            );

            console.log('✅ Pagamento confirmado com sucesso:', response.data);
            alert('Pagamento confirmado com sucesso!');
            await fetchData();
        } catch (e) {
            console.error('❌ Erro ao confirmar pagamento:', e.response?.data || e);
            alert('Erro ao confirmar pagamento: ' + (e.response?.data || e.message));
        }
    }

    async function handleDelete(orderId) {
        console.log('🗑️ handleDelete chamado para:', orderId);

        if (!window.confirm('Tem certeza que deseja excluir esta ordem?')) {
            console.log('   Exclusão cancelada pelo usuário');
            return;
        }

        try {
            console.log(`📤 Enviando DELETE /orders/${orderId}`);
            await axios.delete(`http://localhost:5000/orders/${orderId}`, {
                headers: { Authorization: 'Bearer ' + token }
            });

            console.log('✅ Ordem excluída com sucesso');
            alert('Ordem excluída com sucesso!');
            await fetchData();
        } catch (e) {
            console.error('❌ Erro ao deletar ordem:', e.response?.data || e);
            alert('Erro ao deletar ordem: ' + (e.response?.data || e.message));
        }
    }

    function startEdit(order) {
        console.log('✏️ startEdit chamado');
        console.log('   Ordem selecionada:', order);

        if (!order) {
            console.error('❌ Ordem inválida para edição');
            return;
        }

        setEditingOrder(order);
        setForm({
            vehiclePlate: order.vehiclePlate,
            paymentMethod: order.paymentMethod,
            items: order.items?.map(it => ({
                serviceId: it.serviceId,
                quantity: it.quantity
            })) || []
        });

        console.log('   Formulário preenchido:', {
            vehiclePlate: order.vehiclePlate,
            paymentMethod: order.paymentMethod,
            items: order.items?.length || 0
        });
    }

    return (
        <div>
            <h2>Ordens de Serviço</h2>

            {/* Lista de ordens */}

            <table border="1" cellPadding="4">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Data</th>
                        <th>Cliente</th>
                        <th>Veículo</th>
                        <th>Status</th>
                        <th>Total</th>
                        <th>Pagamento</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {orders.map(o => (
                        <tr key={o.id}>
                            <td>{o.id}</td>
                            <td>{new Date(o.createdAt).toLocaleString()}</td>
                            <td>{o.vehicle?.client?.name} ({o.vehicle?.client?.cpfcnpj})</td>
                            <td>{o.vehiclePlate}</td>
                            <td>{OrderStatusLabel(o.status)}</td>
                            <td>R$ {o.total?.toFixed(2)}</td>
                            <td>{PaymentMethodLabel(o.paymentMethod)}</td>
                            <td>
                                <button
                                    onClick={() => {
                                        console.log('🟢 CLIQUE EDITAR');
                                        console.log('   Ordem:', o);
                                        try {
                                            startEdit(o);
                                            console.log('   startEdit executado sem erro');
                                        } catch (err) {
                                            console.error('   ERRO dentro do onClick Editar:', err);
                                        }
                                    }}
                                    disabled={false}
                                >
                                    Editar
                                </button>

                                <button
                                    onClick={() => {
                                        console.log('🟢 CLIQUE CONFIRMAR PAGAMENTO');
                                        console.log('   Ordem:', o);
                                        try {
                                            handlePay(o);
                                            console.log('   handlePay chamado sem erro síncrono');
                                        } catch (err) {
                                            console.error('   ERRO dentro do onClick Confirmar Pagamento:', err);
                                        }
                                    }}
                                    disabled={false}
                                >
                                    Confirmar Pagamento
                                </button>

                                <button
                                    onClick={() => {
                                        console.log('🟢 CLIQUE EXCLUIR');
                                        console.log('   Ordem:', o);
                                        handleDelete(o.id);
                                    }}
                                >
                                    Excluir
                                </button>
                            </td>
                        </tr>
                    ))}
                    {orders.length === 0 && (
                        <tr>
                            <td colSpan="8">Nenhuma ordem cadastrada.</td>
                        </tr>
                    )}
                </tbody>
            </table >

            <hr />

            {/* Formulário de nova ordem / edição */}
            <h3>{editingOrder ? 'Editar Ordem' : 'Nova Ordem'}</h3>
            <form onSubmit={editingOrder ? handleUpdate : handleCreate}>
                <div>
                    <label>Veículo: </label>
                    {editingOrder ? (
                        <span>{form.vehiclePlate}</span>
                    ) : (
                        <select
                            value={form.vehiclePlate}
                            onChange={e => setForm({ ...form, vehiclePlate: e.target.value })}
                            required
                        >
                            <option value="">Selecione o veículo</option>
                            {vehicles.map(v => (
                                <option key={v.plate} value={v.plate}>
                                    {v.plate} - {v.client?.name}
                                </option>
                            ))}
                        </select>
                    )}
                </div>

                <div>
                    <label>Forma de pagamento: </label>
                    <select
                        value={form.paymentMethod ?? 0}
                        onChange={e =>
                            setForm(f => ({
                                ...f,
                                paymentMethod: Number(e.target.value)
                            }))
                        }
                    >
                        <option value={0}>Dinheiro</option>
                        <option value={1}>Pix</option>
                        <option value={2}>Crédito</option>
                        <option value={3}>Débito</option>
                    </select>
                </div>

                <div>
                    <h4>Serviços</h4>
                    <button type="button" onClick={addItem}>
                        Adicionar serviço
                    </button>
                    {form.items.map((it, index) => (
                        <div key={index} style={{ marginTop: 4 }}>
                            <select
                                value={it.serviceId}
                                onChange={e => updateItem(index, 'serviceId', e.target.value)}
                            >
                                {services.map(s => (
                                    <option key={s.id} value={s.id}>
                                        {s.name}
                                    </option>
                                ))}
                            </select>
                            <input
                                type="number"
                                min="1"
                                value={it.quantity}
                                onChange={e => updateItem(index, 'quantity', e.target.value)}
                                style={{ width: 60, marginLeft: 4 }}
                            />
                            <button type="button" onClick={() => removeItem(index)} style={{ marginLeft: 4 }}>
                                Remover
                            </button>
                        </div>
                    ))}
                    {form.items.length === 0 && <p>Nenhum serviço adicionado.</p>}
                </div>

                <div style={{ marginTop: 8 }}>
                    <button type="submit">
                        {editingOrder ? 'Salvar Alterações' : 'Criar Ordem'}
                    </button>
                    {editingOrder && (
                        <button type="button" onClick={resetForm} style={{ marginLeft: 8 }}>
                            Cancelar Edição
                        </button>
                    )}
                </div>
            </form>
        </div >
    );
}

// Helpers para exibir enums
function OrderStatusLabel(status) {
    switch (status) {
        case 0: return 'Aberta';
        case 1: return 'Paga';
        case 2: return 'Cancelada';
        default: return status;
    }
}

function PaymentMethodLabel(pm) {
    switch (pm) {
        case 0: return 'Dinheiro';
        case 1: return 'Pix';
        case 2: return 'Crédito';
        case 3: return 'Débito';
        default: return pm;
    }
}